using System.Diagnostics;

namespace HttpConnection.Helpers;

internal static class SystemOperations
{
    public static void OpenInBrowser(string uri)
    {
        var info = new ProcessStartInfo(uri) { UseShellExecute = true };

        Process.Start(info);
    }
}
