using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Shiny.Maui.Controls.Desktop.FileDrop;

/// <summary>
/// The small amount of Win32 needed to stop WebView2 consuming a drop before XAML sees it.
/// </summary>
/// <remarks>
/// <para>
/// WinUI's <c>WebView2</c> has no <c>AllowExternalDrop</c> — that property exists on the WPF and
/// WinForms wrappers only. What it does have is a child HWND of its own with an OLE drop target
/// registered on it, and OLE resolves a drop by taking the window under the cursor and walking
/// <em>up</em> its parent chain to the first registered target. Revoking the web view's
/// registration is therefore enough to let the drag reach the XAML island above it, where the
/// element-level handlers are.
/// </para>
/// <para>
/// Revoking a window that has no registration returns a failure HRESULT and changes nothing, so the
/// walk can be indiscriminate about which child windows it tries.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
static partial class FileDropInterop
{
    /// <summary>The window class WebView2 hosts its browser in. Suffixed <c>_0</c> or <c>_1</c> depending on version.</summary>
    const string WebViewWindowClassPrefix = "Chrome_WidgetWin";

    delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr param);

    [LibraryImport("ole32.dll")]
    private static partial int RevokeDragDrop(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc callback, IntPtr param);

    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int GetClassName(IntPtr hwnd, [Out] char[] buffer, int maxCount);

    /// <summary>
    /// Revokes the OLE drop registration on every WebView2 host window under
    /// <paramref name="topLevel"/>. Returns how many were revoked.
    /// </summary>
    public static int RevokeWebViewDropTargets(IntPtr topLevel)
    {
        if (topLevel == IntPtr.Zero)
            return 0;

        var revoked = 0;
        var buffer = new char[256];

        EnumChildWindows(topLevel, (hwnd, _) =>
        {
            var length = GetClassName(hwnd, buffer, buffer.Length);
            if (length > 0 && new string(buffer, 0, length).StartsWith(WebViewWindowClassPrefix, StringComparison.Ordinal))
            {
                if (RevokeDragDrop(hwnd) == 0)
                    revoked++;
            }
            return true;
        }, IntPtr.Zero);

        return revoked;
    }
}
