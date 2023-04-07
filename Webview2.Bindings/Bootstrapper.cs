using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using Webview2.Bindings.Attributes;
using Webview2.Bindings.Models;
using Webview2.Bindings.Models.Abtractions;

namespace Webview2.Bindings;

public class Bootstrapper : IDisposable
{
    #region Singleton

    private static Bootstrapper _instance;

    public static Bootstrapper Instance => _instance ??= new Bootstrapper();

    #endregion

    #region Members

    private IIpc _ipc;
    private BootstrapOptions _options;
    private WebView2 _webView;
    private readonly AutoResetEvent _readyEvent = new(false);
    private readonly object _navigateLock = new();
    private bool _navigationHappened;
    private string _clientPath = string.Empty;
    private readonly SemaphoreSlim _messageTypeRunningLock = new(1, 1);
    private readonly AutoResetEvent _messageProcessingEvent = new(false);
    private readonly Dictionary<string, bool> _messageTypeRunning = new();
    private readonly Dictionary<string, string> _ipcMethods = new();

    #endregion

    #region Props

    private string AppNameLowered => _options.AppName.ToLower();
    private string AppHostName => $"{AppNameLowered}.app";
    private string AppUri => $"http://{AppHostName}/index.html";
    private string AppDevUri => _options.DevUri;
    private static bool IsDev => Environment.GetEnvironmentVariable("ENV") == "dev";
    public bool IsReady { get; private set; }

    #endregion

    #region Public methods

    public void Dispose()
    {
        CleanClient();
    }

    public async Task Bootstrap(WebView2 webView, IIpc ipc, BootstrapOptions options)
    {
        _webView = webView;
        _ipc = ipc;
        _options = options;

        ReadIpcMethods();
        await InitializeWebviewBindings();

        IsReady = true;
        _readyEvent.Set();
    }

    public Task PostMessage(OutputMessage message)
    {
        return Task.Run(() =>
        {
            if (!IsReady) _readyEvent.WaitOne();

            Application.Current.Dispatcher.InvokeAsync(delegate
            {
                _webView?.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(message));
            });
        });
    }

    #endregion

    #region Private methods

    private async Task InitializeWebviewBindings()
    {
        await _webView.EnsureCoreWebView2Async();

        if (IsDev)
        {
            _webView.CoreWebView2.OpenDevToolsWindow();
            _webView.CoreWebView2.Navigate(AppDevUri);
        }
        else
        {
            var clientPath = ExtractClient();

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
    }

    private void ReadIpcMethods()
    {
        foreach (var method in _ipc.GetType().GetMethods())
        {
            var attr = method.GetCustomAttributes(typeof(WebviewInvokable), false)
                .Select(a => (WebviewInvokable)a)
                .FirstOrDefault();

            if (attr is null) continue;

            _ipcMethods[attr.Name] = method.Name;
        }
    }

    private async Task EnterSafeCall(string method)
    {
        while (true)
        {
            await _messageTypeRunningLock.WaitAsync();
            if (!_messageTypeRunning.ContainsKey(method))
            {
                _messageTypeRunning.Add(method, true);
                _messageTypeRunningLock.Release();
                break;
            }

            if (_messageTypeRunning[method])
            {
                _messageTypeRunningLock.Release();
                _messageProcessingEvent.WaitOne();
                continue;
            }

            _messageTypeRunning[method] = true;
            _messageTypeRunningLock.Release();
            break;
        }
    }

    private async Task ExitSafeCall(string method)
    {
        await _messageTypeRunningLock.WaitAsync();
        _messageTypeRunning[method] = false;
        _messageTypeRunningLock.Release();

        _messageProcessingEvent.Set();
    }

    private async Task<OutputMessage?> InvokeMethod(InputMessage message)
    {
        if (!_ipcMethods.ContainsKey(message.Method)) return null;

        var response = _ipc.GetType()
            .GetMethod(_ipcMethods[message.Method])!
            .Invoke(_ipc, new object?[] { message }) as Task<OutputMessage>;

        if (response is null)
        {
            throw new InvalidOperationException("WebviewInvokable methods should return a Task<OutputMessage>");
        }

        return await response;
    }

    private async Task<bool> HandleInputMessage(InputMessage message)
    {
        try
        {
            await EnterSafeCall(message.Method);

            var result = await InvokeMethod(message);

            await ExitSafeCall(message.Method);

            if (result is null) return false;

            await PostMessage(result);
            return true;
        }
        catch (Exception e)
        {
            //Log.Error("[IPC] {Message} {CallStack}", e.Message, IsDev ? e.StackTrace : string.Empty);
        }

        return false;
    }

    private string ExtractClient()
    {
        var clientArchive = $"{_options.AppName}.client";
        if (!File.Exists(clientArchive)) throw new FileNotFoundException("Client file not found");
        _clientPath = Path.Join(Path.GetTempPath(), _options.AppName);

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
        zip.ExtractZip(clientArchive, _clientPath, FastZip.Overwrite.Always, _ => true, string.Empty, string.Empty,
            true);

        return _clientPath;
    }

    private void CleanClient()
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

    private void PreventUnexpectedNavigation(string uri)
    {
        if (IsDev) return;

        lock (_navigateLock)
        {
            if (uri == "about:blank") return;
            if (uri == AppUri && !_navigationHappened)
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

        MessageBox.Show($"{(uri.StartsWith(AppUri) ? "Page reload" : $"Navigation to \"{uri}\"")} is not allowed",
            "Unexpected navigation", MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    #endregion

    #region Webview events

    private void WebView_OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        PreventUnexpectedNavigation(e.Uri);
    }

    private void WebView_OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        InputMessage? message;
        var json = e.WebMessageAsJson;

        try
        {
            message = JsonConvert.DeserializeObject<InputMessage>(json);
            if (message is null)
            {
                //Log.Warning($"Invalid message from client: {e.WebMessageAsJson}");
                return;
            }
        }
        catch (Exception ex)
        {
            // Log.Warning($"Invalid message from client: {json} {ex.Message}");
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                if (await HandleInputMessage(message)) return;

                //Log.Warning("Unexpected webview message {Type}: {Data}", message.Type, message.Data);
                // await PostMessageToWebContent(new WebViewMessageResponse(message, false,
                //     message: "Unexpected message"));
            }
            catch (Exception ex)
            {
                //Log.Warning($"Unable to handle message from client: {json} {ex.Message}");
            }
        });
    }

    #endregion
}