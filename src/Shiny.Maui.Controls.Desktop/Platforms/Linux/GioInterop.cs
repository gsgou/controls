using System.Runtime.InteropServices;

namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// The GDBus and GVariant surface needed to talk to the desktop portal.
/// </summary>
/// <remarks>
/// Deliberately avoids every variadic entry point in GLib. <c>g_variant_new</c> and
/// <c>g_variant_builder_add</c> take a format string plus varargs, and varargs marshalling is not
/// portable across the ABIs this has to run on — one mismatched argument is an abort inside GLib,
/// not a managed exception. Composing variants out of the non-variadic constructors below is
/// slower to write and impossible to get subtly wrong at runtime.
/// </remarks>
static partial class GioInterop
{
    const string Gio = "libgio-2.0.so.0";
    const string GLib = "libglib-2.0.so.0";

    public const int BusTypeSession = 2;

    [LibraryImport(Gio, EntryPoint = "g_bus_get_sync")]
    public static partial IntPtr BusGetSync(int busType, IntPtr cancellable, out IntPtr error);

    [LibraryImport(Gio, EntryPoint = "g_dbus_connection_get_unique_name")]
    public static partial IntPtr ConnectionGetUniqueName(IntPtr connection);

    [LibraryImport(Gio, EntryPoint = "g_dbus_connection_call_sync", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr ConnectionCallSync(
        IntPtr connection,
        string? busName,
        string objectPath,
        string interfaceName,
        string methodName,
        IntPtr parameters,
        IntPtr replyType,
        int flags,
        int timeoutMsec,
        IntPtr cancellable,
        out IntPtr error
    );

    [LibraryImport(Gio, EntryPoint = "g_dbus_connection_signal_subscribe", StringMarshalling = StringMarshalling.Utf8)]
    public static partial uint ConnectionSignalSubscribe(
        IntPtr connection,
        string? sender,
        string? interfaceName,
        string? member,
        string? objectPath,
        string? arg0,
        int flags,
        IntPtr callback,
        IntPtr userData,
        IntPtr userDataFreeFunc
    );

    [LibraryImport(Gio, EntryPoint = "g_dbus_connection_signal_unsubscribe")]
    public static partial void ConnectionSignalUnsubscribe(IntPtr connection, uint subscriptionId);

    [LibraryImport(GLib, EntryPoint = "g_variant_type_new", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr VariantTypeNew(string type);

    [LibraryImport(GLib, EntryPoint = "g_variant_type_free")]
    public static partial void VariantTypeFree(IntPtr type);

    [LibraryImport(GLib, EntryPoint = "g_variant_new_string", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr VariantNewString(string value);

    [LibraryImport(GLib, EntryPoint = "g_variant_new_object_path", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr VariantNewObjectPath(string value);

    [LibraryImport(GLib, EntryPoint = "g_variant_new_variant")]
    public static partial IntPtr VariantNewVariant(IntPtr value);

    [LibraryImport(GLib, EntryPoint = "g_variant_new_dict_entry")]
    public static partial IntPtr VariantNewDictEntry(IntPtr key, IntPtr value);

    [LibraryImport(GLib, EntryPoint = "g_variant_new_tuple")]
    public static partial IntPtr VariantNewTuple(IntPtr[] children, nuint count);

    [LibraryImport(GLib, EntryPoint = "g_variant_builder_new")]
    public static partial IntPtr VariantBuilderNew(IntPtr type);

    [LibraryImport(GLib, EntryPoint = "g_variant_builder_add_value")]
    public static partial void VariantBuilderAddValue(IntPtr builder, IntPtr value);

    [LibraryImport(GLib, EntryPoint = "g_variant_builder_end")]
    public static partial IntPtr VariantBuilderEnd(IntPtr builder);

    [LibraryImport(GLib, EntryPoint = "g_variant_builder_unref")]
    public static partial void VariantBuilderUnref(IntPtr builder);

    [LibraryImport(GLib, EntryPoint = "g_variant_get_child_value")]
    public static partial IntPtr VariantGetChildValue(IntPtr value, nuint index);

    [LibraryImport(GLib, EntryPoint = "g_variant_get_string")]
    public static partial IntPtr VariantGetString(IntPtr value, IntPtr length);

    [LibraryImport(GLib, EntryPoint = "g_variant_lookup_value", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr VariantLookupValue(IntPtr dictionary, string key, IntPtr expectedType);

    [LibraryImport(GLib, EntryPoint = "g_variant_get_uint32")]
    public static partial uint VariantGetUInt32(IntPtr value);

    [LibraryImport(GLib, EntryPoint = "g_variant_unref")]
    public static partial void VariantUnref(IntPtr value);

    [LibraryImport(GLib, EntryPoint = "g_error_free")]
    public static partial void ErrorFree(IntPtr error);

    public static string? ReadString(IntPtr variant)
    {
        if (variant == IntPtr.Zero)
            return null;

        var ptr = VariantGetString(variant, IntPtr.Zero);
        return ptr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }
}
