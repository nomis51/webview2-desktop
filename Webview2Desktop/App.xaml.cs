using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Webview2Desktop.WebView;

namespace Webview2Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public App()
    {
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        Exit += OnExit;
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        WebViewBootstrapper.Dispose();
    }
}