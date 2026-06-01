using System.Runtime.InteropServices;

namespace Shiny.Maui.Controls.TrayIcon;

/// <summary>
/// P/Invoke into libayatana-appindicator3 and GTK 3. Requires the user to install:
/// libayatana-appindicator3-1 (Debian/Ubuntu) or libayatana-appindicator3 (Fedora/Arch)
/// plus libgtk-3-0. GTK must be initialized once (gtk_init) before any tray icon is created;
/// LinuxTrayIcon.EnsureInitialized handles this.
/// </summary>
static partial class LinuxInterop
{
    const string AppIndicator = "libayatana-appindicator3.so.1";
    const string Gtk = "libgtk-3.so.0";
    const string GObject = "libgobject-2.0.so.0";
    const string GLib = "libglib-2.0.so.0";

    public enum AppIndicatorCategory
    {
        ApplicationStatus = 0,
        Communications,
        SystemServices,
        Hardware,
        Other
    }

    public enum AppIndicatorStatus
    {
        Passive = 0,
        Active,
        Attention
    }

    [LibraryImport(AppIndicator, EntryPoint = "app_indicator_new", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr AppIndicatorNew(string id, string iconName, int category);

    [LibraryImport(AppIndicator, EntryPoint = "app_indicator_set_status")]
    public static partial void AppIndicatorSetStatus(IntPtr self, int status);

    [LibraryImport(AppIndicator, EntryPoint = "app_indicator_set_icon_full", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void AppIndicatorSetIconFull(IntPtr self, string iconName, string iconDesc);

    [LibraryImport(AppIndicator, EntryPoint = "app_indicator_set_title", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void AppIndicatorSetTitle(IntPtr self, string title);

    [LibraryImport(AppIndicator, EntryPoint = "app_indicator_set_label", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void AppIndicatorSetLabel(IntPtr self, string label, string guide);

    [LibraryImport(AppIndicator, EntryPoint = "app_indicator_set_menu")]
    public static partial void AppIndicatorSetMenu(IntPtr self, IntPtr menu);

    [LibraryImport(AppIndicator, EntryPoint = "app_indicator_set_secondary_activate_target")]
    public static partial void AppIndicatorSetSecondaryActivateTarget(IntPtr self, IntPtr menuItem);

    [LibraryImport(Gtk, EntryPoint = "gtk_init_check")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool GtkInitCheck(ref int argc, ref IntPtr argv);

    [LibraryImport(Gtk, EntryPoint = "gtk_main_iteration")]
    public static partial void GtkMainIteration();

    [LibraryImport(Gtk, EntryPoint = "gtk_events_pending")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool GtkEventsPending();

    [LibraryImport(Gtk, EntryPoint = "gtk_menu_new")]
    public static partial IntPtr GtkMenuNew();

    [LibraryImport(Gtk, EntryPoint = "gtk_menu_item_new_with_label", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr GtkMenuItemNewWithLabel(string label);

    [LibraryImport(Gtk, EntryPoint = "gtk_check_menu_item_new_with_label", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr GtkCheckMenuItemNewWithLabel(string label);

    [LibraryImport(Gtk, EntryPoint = "gtk_check_menu_item_set_active")]
    public static partial void GtkCheckMenuItemSetActive(IntPtr item, [MarshalAs(UnmanagedType.U1)] bool active);

    [LibraryImport(Gtk, EntryPoint = "gtk_check_menu_item_get_active")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool GtkCheckMenuItemGetActive(IntPtr item);

    [LibraryImport(Gtk, EntryPoint = "gtk_separator_menu_item_new")]
    public static partial IntPtr GtkSeparatorMenuItemNew();

    [LibraryImport(Gtk, EntryPoint = "gtk_menu_item_set_submenu")]
    public static partial void GtkMenuItemSetSubmenu(IntPtr item, IntPtr submenu);

    [LibraryImport(Gtk, EntryPoint = "gtk_menu_shell_append")]
    public static partial void GtkMenuShellAppend(IntPtr shell, IntPtr child);

    [LibraryImport(Gtk, EntryPoint = "gtk_widget_show_all")]
    public static partial void GtkWidgetShowAll(IntPtr widget);

    [LibraryImport(Gtk, EntryPoint = "gtk_widget_show")]
    public static partial void GtkWidgetShow(IntPtr widget);

    [LibraryImport(Gtk, EntryPoint = "gtk_widget_set_sensitive")]
    public static partial void GtkWidgetSetSensitive(IntPtr widget, [MarshalAs(UnmanagedType.U1)] bool sensitive);

    [LibraryImport(GObject, EntryPoint = "g_signal_connect_data", StringMarshalling = StringMarshalling.Utf8)]
    public static partial ulong GSignalConnectData(IntPtr instance, string signalName, IntPtr handler, IntPtr data, IntPtr destroyData, int connectFlags);

    [LibraryImport(GObject, EntryPoint = "g_object_ref_sink")]
    public static partial IntPtr GObjectRefSink(IntPtr obj);

    [LibraryImport(GObject, EntryPoint = "g_object_unref")]
    public static partial void GObjectUnref(IntPtr obj);

    // gtk_init_check sometimes lives in GLib in some versions — keep handy
    [LibraryImport(GLib, EntryPoint = "g_main_context_iteration")]
    public static partial int GMainContextIteration(IntPtr ctx, [MarshalAs(UnmanagedType.U1)] bool mayBlock);
}
