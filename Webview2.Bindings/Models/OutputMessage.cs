namespace Webview2.Bindings.Models;

public class OutputMessage
{
    public string Id { get; }
    public string Type { get; }
    public object? Data { get; }

    public OutputMessage(InputMessage inputMessage, object? data = null)
    {
        Id = inputMessage.Id;
        Data = data;
    }

    public OutputMessage(string type, object? data = null)
    {
        Type = type;
        Data = data;
    }
}