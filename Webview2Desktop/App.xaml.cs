using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Webview2.Bindings;

namespace Webview2Desktop;

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
        Bootstrapper.Instance.Dispose();
    }
}