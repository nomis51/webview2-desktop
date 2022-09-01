using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Webview2Desktop.Models.Abstraction;

namespace Webview2Desktop.Models;

public class WebContentMessage
{
    public string Type { get; set; }

    public string? Data { get; set; }

    [JsonIgnore] public JObject? DataAsJson => string.IsNullOrEmpty(Data) ? null : JsonConvert.DeserializeObject(Data) as JObject;

    public WebContentMessage(string type, object? data = null)
    {
        Type = type;
        Data = data switch
        {
            null => null,
            ISerializableData => JsonConvert.SerializeObject(data),
            _ => data.ToString()
        };
    }
}