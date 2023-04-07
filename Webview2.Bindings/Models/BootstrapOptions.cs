namespace Webview2.Bindings.Models;

public class BootstrapOptions
{
    public string AppName { get; }
    public string DevUri { get; }

    public BootstrapOptions(string appName, string devUri)
    {
        AppName = appName;
        DevUri = devUri;
    }
}