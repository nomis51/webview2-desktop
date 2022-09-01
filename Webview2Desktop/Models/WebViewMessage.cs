namespace Webview2Desktop.Models;

public class WebViewMessage
{
    public string Type { get; set; }

    public object? Data { get; set; }

    public WebViewMessage(string type, object? data = null)
    {
        Type = type;
        Data = data;
    }
}