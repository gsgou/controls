using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Shiny.Maui.Controls.Desktop.TrayIcon;

namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// Wayland global hotkeys through <c>org.freedesktop.portal.GlobalShortcuts</c>.
/// </summary>
/// <remarks>
/// <para>
/// The portal is the only sanctioned way to get a system-wide key on Wayland — a client cannot
/// grab the keyboard itself. It is implemented by GNOME 45+ and KDE Plasma 6+; on a compositor
/// without it <see cref="TryCreate"/> returns null and the caller falls back to a tray icon.
/// </para>
/// <para>
/// Two properties of the portal are worth knowing before you rely on it. Binding shows the user a
/// system confirmation dialog, so the hotkey starts working only once they accept — asynchronously,
/// after startup. And the trigger passed here is a <em>preference</em>: the compositor is free to
/// bind something else, and the user can rebind it in their settings, so never present the
/// configured accelerator as fact on Wayland.
/// </para>
/// </remarks>
sealed unsafe class PortalHotKeyBackend : ILinuxHotKeyBackend, IDisposable
{
    const string PortalBus = "org.freedesktop.portal.Desktop";
    const string PortalPath = "/org/freedesktop/portal/desktop";
    const string ShortcutsInterface = "org.freedesktop.portal.GlobalShortcuts";
    const string RequestInterface = "org.freedesktop.portal.Request";

    static PortalHotKeyBackend? current;

    readonly ILogger? logger;
    readonly IntPtr connection;
    string sessionHandle = String.Empty;
    readonly Dictionary<string, Action> handlers = new();
    readonly List<(string Id, string Trigger)> bound = new();
    readonly object gate = new();

    uint activatedSubscription;
    uint responseSubscription;
    int nextShortcutId;

    // Response signals are consumed by whichever request is currently waiting; the portal only
    // ever has one of ours in flight because binding is serialised behind this gate.
    readonly object requestGate = new();
    TaskCompletionSource<IntPtr>? pendingResponse;
    string? pendingRequestPath;

    PortalHotKeyBackend(IntPtr connection, ILogger? logger)
    {
        this.connection = connection;
        this.logger = logger;
    }

    public static PortalHotKeyBackend? TryCreate(ILogger? logger)
    {
        try
        {
            var connection = GioInterop.BusGetSync(GioInterop.BusTypeSession, IntPtr.Zero, out var error);
            if (error != IntPtr.Zero)
            {
                GioInterop.ErrorFree(error);
                return null;
            }
            if (connection == IntPtr.Zero)
                return null;

            // `current` has to be live before CreateSession runs: the portal answers on the
            // Response signal, and the static callback resolves the waiting backend through it.
            var backend = new PortalHotKeyBackend(connection, logger);
            current = backend;

            var session = backend.CreateSession();
            if (session == null)
            {
                backend.Dispose();
                return null;
            }

            backend.sessionHandle = session;
            backend.SubscribeActivated();
            return backend;
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "GlobalShortcuts portal unavailable");
            return null;
        }
    }

    public IDisposable? Register(TrayAccelerator accelerator, Action pressed)
    {
        var trigger = BuildTrigger(accelerator);
        if (trigger == null)
            return null;

        var id = "shiny.quickentry." + Interlocked.Increment(ref this.nextShortcutId);

        lock (this.gate)
        {
            this.handlers[id] = pressed;
            this.bound.Add((id, trigger));
        }

        if (!this.BindShortcuts())
        {
            lock (this.gate)
            {
                this.handlers.Remove(id);
                this.bound.RemoveAll(b => b.Id == id);
            }
            return null;
        }

        return new Registration(this, id);
    }

    // -------------------------------------------------------------------------------------
    // Portal calls
    // -------------------------------------------------------------------------------------

    string? CreateSession()
    {
        this.SubscribeResponses();

        var token = "shiny" + Environment.ProcessId + "_" + Interlocked.Increment(ref this.nextShortcutId);
        var options = BuildDictionary(
            ("handle_token", GioInterop.VariantNewString(token)),
            ("session_handle_token", GioInterop.VariantNewString(token + "s"))
        );

        var results = this.CallAndAwaitResponse("CreateSession", new[] { options });
        if (results == IntPtr.Zero)
            return null;

        try
        {
            var handleType = GioInterop.VariantTypeNew("s");
            var handle = GioInterop.VariantLookupValue(results, "session_handle", handleType);
            GioInterop.VariantTypeFree(handleType);

            if (handle == IntPtr.Zero)
                return null;

            var value = GioInterop.ReadString(handle);
            GioInterop.VariantUnref(handle);
            return value;
        }
        finally
        {
            GioInterop.VariantUnref(results);
        }
    }

    bool BindShortcuts()
    {
        (string Id, string Trigger)[] shortcuts;
        lock (this.gate)
            shortcuts = this.bound.ToArray();

        var listType = GioInterop.VariantTypeNew("a(sa{sv})");
        var builder = GioInterop.VariantBuilderNew(listType);
        GioInterop.VariantTypeFree(listType);

        foreach (var (id, trigger) in shortcuts)
        {
            var options = BuildDictionary(
                ("description", GioInterop.VariantNewString("Open the quick entry popup")),
                ("preferred_trigger", GioInterop.VariantNewString(trigger))
            );
            var entry = GioInterop.VariantNewTuple(new[] { GioInterop.VariantNewString(id), options }, 2);
            GioInterop.VariantBuilderAddValue(builder, entry);
        }

        var list = GioInterop.VariantBuilderEnd(builder);
        var token = "shinybind" + Interlocked.Increment(ref this.nextShortcutId);
        var callOptions = BuildDictionary(("handle_token", GioInterop.VariantNewString(token)));

        var args = new[]
        {
            GioInterop.VariantNewObjectPath(this.sessionHandle),
            list,
            GioInterop.VariantNewString(String.Empty),   // parent_window — none, we have no XDG surface handle
            callOptions
        };

        var results = this.CallAndAwaitResponse("BindShortcuts", args);
        if (results == IntPtr.Zero)
            return false;

        GioInterop.VariantUnref(results);
        return true;
    }

    /// <summary>
    /// Invokes a portal method and waits for the asynchronous <c>Response</c> that carries its real
    /// result. Every portal method returns only a request handle synchronously — the outcome, and
    /// anything the user was asked to confirm, arrives later on that request object.
    /// </summary>
    IntPtr CallAndAwaitResponse(string method, IntPtr[] args)
    {
        lock (this.requestGate)
        {
            var completion = new TaskCompletionSource<IntPtr>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.pendingResponse = completion;
            this.pendingRequestPath = null;

            var parameters = GioInterop.VariantNewTuple(args, (nuint)args.Length);
            var reply = GioInterop.ConnectionCallSync(
                this.connection, PortalBus, PortalPath, ShortcutsInterface, method,
                parameters, IntPtr.Zero, 0, 30_000, IntPtr.Zero, out var error
            );

            if (error != IntPtr.Zero)
            {
                GioInterop.ErrorFree(error);
                this.pendingResponse = null;
                return IntPtr.Zero;
            }

            if (reply == IntPtr.Zero)
            {
                this.pendingResponse = null;
                return IntPtr.Zero;
            }

            var handleVariant = GioInterop.VariantGetChildValue(reply, 0);
            this.pendingRequestPath = GioInterop.ReadString(handleVariant);
            GioInterop.VariantUnref(handleVariant);
            GioInterop.VariantUnref(reply);

            // Binding pops a confirmation dialog the user has to accept, so the wait is generous.
            if (!completion.Task.Wait(TimeSpan.FromMinutes(2)))
            {
                this.pendingResponse = null;
                this.logger?.LogWarning("The GlobalShortcuts portal did not answer {Method} in time.", method);
                return IntPtr.Zero;
            }

            this.pendingResponse = null;
            return completion.Task.Result;
        }
    }

    // -------------------------------------------------------------------------------------
    // Signals
    // -------------------------------------------------------------------------------------

    void SubscribeResponses()
    {
        delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void> callback = &OnResponse;
        this.responseSubscription = GioInterop.ConnectionSignalSubscribe(
            this.connection, PortalBus, RequestInterface, "Response", null, null, 0,
            (IntPtr)callback, IntPtr.Zero, IntPtr.Zero
        );
    }

    void SubscribeActivated()
    {
        delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void> callback = &OnActivated;
        this.activatedSubscription = GioInterop.ConnectionSignalSubscribe(
            this.connection, PortalBus, ShortcutsInterface, "Activated", null, null, 0,
            (IntPtr)callback, IntPtr.Zero, IntPtr.Zero
        );
    }

    [UnmanagedCallersOnly]
    static void OnResponse(IntPtr connection, IntPtr sender, IntPtr objectPath, IntPtr interfaceName, IntPtr signalName, IntPtr parameters, IntPtr userData)
    {
        try
        {
            var backend = current;
            if (backend?.pendingResponse == null)
                return;

            var path = Marshal.PtrToStringUTF8(objectPath);
            if (backend.pendingRequestPath != null && path != backend.pendingRequestPath)
                return;

            // (u response, a{sv} results) — response 0 is success, 1 cancelled, 2 failed.
            var codeVariant = GioInterop.VariantGetChildValue(parameters, 0);
            var code = GioInterop.VariantGetUInt32(codeVariant);
            GioInterop.VariantUnref(codeVariant);

            if (code != 0)
            {
                backend.pendingResponse.TrySetResult(IntPtr.Zero);
                return;
            }

            // Handed to the waiter, which owns the reference and unrefs it.
            var results = GioInterop.VariantGetChildValue(parameters, 1);
            backend.pendingResponse.TrySetResult(results);
        }
        catch
        {
            // Never unwind into the GLib main loop.
        }
    }

    [UnmanagedCallersOnly]
    static void OnActivated(IntPtr connection, IntPtr sender, IntPtr objectPath, IntPtr interfaceName, IntPtr signalName, IntPtr parameters, IntPtr userData)
    {
        try
        {
            var backend = current;
            if (backend == null)
                return;

            // (o session_handle, s shortcut_id, t timestamp, a{sv} options)
            var idVariant = GioInterop.VariantGetChildValue(parameters, 1);
            var id = GioInterop.ReadString(idVariant);
            GioInterop.VariantUnref(idVariant);

            if (id == null)
                return;

            Action? action;
            lock (backend.gate)
                backend.handlers.TryGetValue(id, out action);

            action?.Invoke();
        }
        catch
        {
        }
    }

    // -------------------------------------------------------------------------------------

    static IntPtr BuildDictionary(params (string Key, IntPtr Value)[] entries)
    {
        var type = GioInterop.VariantTypeNew("a{sv}");
        var builder = GioInterop.VariantBuilderNew(type);
        GioInterop.VariantTypeFree(type);

        foreach (var (key, value) in entries)
        {
            var entry = GioInterop.VariantNewDictEntry(
                GioInterop.VariantNewString(key),
                GioInterop.VariantNewVariant(value)
            );
            GioInterop.VariantBuilderAddValue(builder, entry);
        }

        return GioInterop.VariantBuilderEnd(builder);
    }

    /// <summary>
    /// Builds the portal's trigger syntax — modifier names in caps joined with '+', then the key.
    /// This is a hint only; the compositor decides what is actually bound.
    /// </summary>
    static string? BuildTrigger(TrayAccelerator accelerator)
    {
        var parts = new List<string>();
        if ((accelerator.Modifiers & TrayAcceleratorModifiers.Control) != 0) parts.Add("CTRL");
        if ((accelerator.Modifiers & TrayAcceleratorModifiers.Alt) != 0) parts.Add("ALT");
        if ((accelerator.Modifiers & TrayAcceleratorModifiers.Shift) != 0) parts.Add("SHIFT");
        if ((accelerator.Modifiers & TrayAcceleratorModifiers.Meta) != 0) parts.Add("SUPER");

        if (parts.Count == 0)
            return null;

        var key = accelerator.Key.ToLowerInvariant() switch
        {
            "space" or "spacebar" => "space",
            "enter" or "return" => "Return",
            "esc" or "escape" => "Escape",
            "tab" => "Tab",
            var other => other
        };

        parts.Add(key);
        return String.Join('+', parts);
    }

    void Unregister(string id)
    {
        lock (this.gate)
        {
            this.handlers.Remove(id);
            this.bound.RemoveAll(b => b.Id == id);
        }
        // The portal has no unbind: shortcuts live for the session's lifetime. Dropping the handler
        // makes the key inert, and the session goes away with the process.
    }

    public void Dispose()
    {
        if (this.activatedSubscription != 0)
            GioInterop.ConnectionSignalUnsubscribe(this.connection, this.activatedSubscription);
        if (this.responseSubscription != 0)
            GioInterop.ConnectionSignalUnsubscribe(this.connection, this.responseSubscription);

        if (ReferenceEquals(current, this))
            current = null;
    }

    sealed class Registration : IDisposable
    {
        readonly PortalHotKeyBackend owner;
        readonly string id;
        bool disposed;

        public Registration(PortalHotKeyBackend owner, string id)
        {
            this.owner = owner;
            this.id = id;
        }

        public void Dispose()
        {
            if (this.disposed)
                return;
            this.disposed = true;
            this.owner.Unregister(this.id);
        }
    }
}
