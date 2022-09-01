using System.Threading.Tasks;
using Webview2Desktop.Models;
using Webview2Desktop.WebView;

namespace Webview2Desktop.ViewModels;

public class AppViewModel
{
    public delegate void MessageEvent(string message);

    public event MessageEvent OnMessageEvent;
    
    public AppViewModel()
    {
        WebViewBootstrapper.OnMessageForCSharp += OnMessageForCSharp;
    }

    public async Task TalkToAngular(string message)
    {
        await WebViewBootstrapper.PostMessageToWebContent(new WebViewMessage(WebContentCommands.MessageForAngular, message));
    }

    private void OnMessageForCSharp(string message)
    {
       OnMessageEvent?.Invoke(message);
    }
}