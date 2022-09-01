using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using Webview2Desktop.Models;

namespace Webview2Desktop.WebView;

public class WebViewBootstrapper
{
    public delegate void WebContentMessageReceivedEvent(WebContentMessage message);

    public static event WebContentMessageReceivedEvent OnWebContentMessageReceived;

    public delegate void MessageForCSharpEvent(string message);

    public static event MessageForCSharpEvent OnMessageForCSharp;


    private static WebView2? _webView;
    private static readonly AutoResetEvent ReadyEvent = new(false);


    private static bool IsDev => Environment.GetEnvironmentVariable("ENV") == "dev";
    private static bool IsPreProd => Environment.GetEnvironmentVariable("ENV") == "preprod";

    public static bool IsReady { get; private set; }


    public static async Task Bootstrap(WebView2 webView)
    {
        _webView = webView;

        await _webView.EnsureCoreWebView2Async();

        if (IsDev)
        {
            _webView.CoreWebView2.OpenDevToolsWindow();
            // TODO: Change the port to match the one used by your JS framework. e.g. 8080 for Vue, 4200 for Angular, etc.
            _webView.CoreWebView2.Navigate("http://localhost:4200");
        }
        else
        {
            // TODO: change "Webview2Desktop" for your app name
            // preprod let you test your client app production build with a dev build of your backend
            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "webview2desktop.app",
                Path.GetFullPath(IsPreProd ? "../Webview2Desktop.Client/dist" : "./Webview2Desktop.Client"),
                CoreWebView2HostResourceAccessKind.DenyCors
            );
            _webView.Source = new Uri("http://webview2desktop.app/index.html");
        }

        _webView.CoreWebView2.WebMessageReceived += WebView_OnWebMessageReceived;
        IsReady = true;
        ReadyEvent.Set();
    }

    public static Task PostMessageToWebContent(WebViewMessage message)
    {
        return Task.Run(() =>
        {
            if (!IsReady) ReadyEvent.WaitOne();

            System.Windows.Application.Current.Dispatcher.InvokeAsync(delegate { _webView?.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(message)); });
        });
    }


    private static void WebView_OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = JsonConvert.DeserializeObject<WebContentMessage>(e.WebMessageAsJson);
            if (message is null)
            {
                Debug.WriteLine($"Invalid message from client: {e.WebMessageAsJson}");
                return;
            }

            if (HandleMessage(message)) return;

            // Generic message not handled with custom workflow
            OnWebContentMessageReceived?.Invoke(message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Invalid message from client: {e.WebMessageAsJson} {ex.Message}");
        }
    }

    private static bool HandleMessage(WebContentMessage message)
    {
        // Define here custom workflows for specific messages
        try
        {
            switch (message.Type)
            {
                case WebViewCommands.MessageForCSharp:
                    OnMessageForCSharp?.Invoke(message.Data!);
                    return true;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        return false;
    }
}