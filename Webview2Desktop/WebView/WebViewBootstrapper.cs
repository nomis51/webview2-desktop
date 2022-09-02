using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using Webview2Desktop.Models;

namespace Webview2Desktop.WebView;

public static class WebViewBootstrapper
{
    private const string AppName = "Webview2Desktop";
    private static readonly string AppNameLowered = AppName.ToLower();
    private static readonly string AppHostName = $"{AppNameLowered}.app";
    private static readonly string AppUri = $"http://{AppHostName}/index.html";
    private const string DevAppUri = "http://localhost:4200";
    private const string DevDistFolder = $"../{AppName}.Client/dist";

    public delegate void WebContentMessageReceivedEvent(WebContentMessage message);

    public static event WebContentMessageReceivedEvent OnWebContentMessageReceived;

    public delegate void MessageForCSharpEvent(string message);

    public static event MessageForCSharpEvent OnMessageForCSharp;

    private static WebView2? _webView;
    private static readonly AutoResetEvent ReadyEvent = new(false);
    private static object _navigateLock = new();
    private static bool _navigationHappened;
    private static string _clientPath = string.Empty;

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
            _webView.CoreWebView2.Navigate(DevAppUri);
        }
        else
        {
            // TODO: change "Webview2Desktop" for your app name
            var clientPath = IsPreProd ? DevDistFolder : ExtractClient();

            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                AppHostName,
                Path.GetFullPath(clientPath),
                CoreWebView2HostResourceAccessKind.DenyCors
            );
            _webView.Source = new Uri(AppUri);
        }

        _webView.CoreWebView2.WebMessageReceived += WebView_OnWebMessageReceived;
        _webView.CoreWebView2.NavigationStarting += WebView_OnNavigationStarting;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = IsDev;
        IsReady = true;
        ReadyEvent.Set();
    }

    public static void Dispose()
    {
        CleanClient();
    }

    public static Task PostMessageToWebContent(WebViewMessage message)
    {
        return Task.Run(() =>
        {
            if (!IsReady) ReadyEvent.WaitOne();

            Application.Current.Dispatcher.InvokeAsync(delegate { _webView?.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(message)); });
        });
    }

    private static void WebView_OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (IsDev) return;

        lock (_navigateLock)
        {
            if (e.Uri == "about:blank") return;
            if (e.Uri == AppUri && !_navigationHappened)
            {
                _navigationHappened = true;
                return;
            }

            _webView!.CoreWebView2.SetVirtualHostNameToFolderMapping(
                AppHostName,
                Path.GetFullPath("./"),
                CoreWebView2HostResourceAccessKind.DenyCors
            );
            _webView.CoreWebView2.Navigate("about:blank");

            var clientPath = ExtractClient();
            _webView!.CoreWebView2.SetVirtualHostNameToFolderMapping(
                AppHostName,
                Path.GetFullPath(clientPath),
                CoreWebView2HostResourceAccessKind.DenyCors
            );
            _navigationHappened = false;
            _webView.CoreWebView2.Navigate(AppUri);
        }

        MessageBox.Show($"{(e.Uri.StartsWith(AppUri) ? "Page reload" : $"Navigation to \"{e.Uri}\"")} is not allowed", "Unexpected navigation", MessageBoxButton.OK,
            MessageBoxImage.Warning);
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

    private static string ExtractClient()
    {
        var clientArchive = $"{AppName}.client";
        if (!File.Exists(clientArchive)) throw new FileNotFoundException("Client file not found");
        _clientPath = Path.Join(Path.GetTempPath(), AppName);

        try
        {
            if (Directory.Exists(_clientPath)) Directory.Delete(_clientPath, true);
        }
        catch (Exception)
        {
            _clientPath += $"/{Guid.NewGuid()}";
        }

        var zip = new FastZip
        {
            Password = "#{APP_ID}",
            CompressionLevel = Deflater.CompressionLevel.BEST_COMPRESSION
        };
        zip.ExtractZip(clientArchive, _clientPath, FastZip.Overwrite.Always, _ => true, string.Empty, string.Empty, true);

        return _clientPath;
    }

    private static void CleanClient()
    {
        try
        {
            if (Directory.Exists(_clientPath)) Directory.Delete(_clientPath, true);
        }
        catch (Exception)
        {
            // ignored
        }
    }
}