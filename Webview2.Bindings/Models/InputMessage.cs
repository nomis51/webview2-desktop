using Newtonsoft.Json.Linq;

namespace Webview2.Bindings.Models;

public class InputMessage
{
    public string Id { get; set; }
    public string Method { get; set; }
    public JObject? Data { get; set; }
}