using System.Runtime.InteropServices;

namespace Shiny.Maui.Controls.Desktop.FileDrop;

/// <summary>
/// P/Invoke into GTK 4, GDK, GObject and GIO for window-level file drop.
/// </summary>
/// <remarks>
/// Kept apart from the quick entry popup's <c>Gtk4Interop</c> — same libraries, unrelated feature,
/// and that one lives in the quick entry namespace. Only <c>g_signal_connect_data</c> and
/// <c>gtk_widget_add_controller</c> overlap, which is a cheaper duplication than a shared file that
/// belongs to neither.
/// </remarks>
static partial class Gtk4DropInterop
{
    const string Gtk = "libgtk-4.so.1";
    const string GObject = "libgobject-2.0.so.0";
    const string GLib = "libglib-2.0.so.0";
    const string Gio = "libgio-2.0.so.0";

    /// <summary>GDK_ACTION_COPY.</summary>
    public const uint ActionCopy = 1 << 0;

    /// <summary>
    /// GTK_PHASE_CAPTURE — the controller sees the event before any child does, which is what puts
    /// this above a WebKitWebView rather than behind it.
    /// </summary>
    public const int PhaseCapture = 1;

    /// <summary>GTK_PHASE_BUBBLE — children get first refusal, the default for event controllers.</summary>
    public const int PhaseBubble = 2;

    [LibraryImport(Gtk, EntryPoint = "gtk_drop_target_new")]
    public static partial IntPtr DropTargetNew(nuint type, uint actions);

    /// <summary>
    /// Makes the payload readable while the drag is still moving rather than only on drop, which is
    /// what lets the hover events name the files.
    /// </summary>
    [LibraryImport(Gtk, EntryPoint = "gtk_drop_target_set_preload")]
    public static partial void DropTargetSetPreload(IntPtr target, [MarshalAs(UnmanagedType.I4)] int preload);

    /// <summary>The current value, or NULL until preloading has finished.</summary>
    [LibraryImport(Gtk, EntryPoint = "gtk_drop_target_get_value")]
    public static partial IntPtr DropTargetGetValue(IntPtr target);

    [LibraryImport(Gtk, EntryPoint = "gtk_event_controller_set_propagation_phase")]
    public static partial void EventControllerSetPropagationPhase(IntPtr controller, int phase);

    [LibraryImport(Gtk, EntryPoint = "gtk_widget_add_controller")]
    public static partial void WidgetAddController(IntPtr widget, IntPtr controller);

    [LibraryImport(Gtk, EntryPoint = "gtk_widget_remove_controller")]
    public static partial void WidgetRemoveController(IntPtr widget, IntPtr controller);

    [LibraryImport(Gtk, EntryPoint = "gdk_file_list_get_type")]
    public static partial nuint FileListGetType();

    /// <summary>Borrowed GSList of GFile — neither the list nor its elements are owned by the caller.</summary>
    [LibraryImport(Gtk, EntryPoint = "gdk_file_list_get_files")]
    public static partial IntPtr FileListGetFiles(IntPtr fileList);

    [LibraryImport(Gio, EntryPoint = "g_file_get_path")]
    public static partial IntPtr FileGetPath(IntPtr file);

    [LibraryImport(GObject, EntryPoint = "g_value_get_boxed")]
    public static partial IntPtr ValueGetBoxed(IntPtr value);

    [LibraryImport(GLib, EntryPoint = "g_free")]
    public static partial void Free(IntPtr mem);

    [LibraryImport(GObject, EntryPoint = "g_signal_connect_data", StringMarshalling = StringMarshalling.Utf8)]
    public static partial ulong SignalConnectData(IntPtr instance, string signal, IntPtr handler, IntPtr data, IntPtr destroy, int flags);

    [LibraryImport(GObject, EntryPoint = "g_object_unref")]
    public static partial void ObjectUnref(IntPtr obj);

    /// <summary>
    /// Walks a GSList of GFile into managed paths.
    /// </summary>
    /// <remarks>
    /// GSList is <c>{ gpointer data; GSList *next; }</c>, so the walk is two pointer reads per node
    /// and needs no struct binding. The strings <c>g_file_get_path</c> returns are owned by the
    /// caller and freed here; the list and the GFiles are not.
    /// </remarks>
    public static List<string> ReadPaths(IntPtr fileList)
    {
        var paths = new List<string>();
        if (fileList == IntPtr.Zero)
            return paths;

        var node = FileListGetFiles(fileList);
        while (node != IntPtr.Zero)
        {
            var file = Marshal.ReadIntPtr(node);
            if (file != IntPtr.Zero)
            {
                var raw = FileGetPath(file);
                if (raw != IntPtr.Zero)
                {
                    // A GFile that is not local (a URI in a remote mount) has no path and returns
                    // NULL rather than an empty string, which is why the null check is not enough on
                    // its own to tell "no files" from "nothing droppable".
                    var path = Marshal.PtrToStringUTF8(raw);
                    Free(raw);

                    if (!String.IsNullOrEmpty(path))
                        paths.Add(path);
                }
            }

            node = Marshal.ReadIntPtr(node, IntPtr.Size);
        }

        return paths;
    }
}
