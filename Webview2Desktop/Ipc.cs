using System;
using System.Threading.Tasks;
using Webview2.Bindings.Attributes;
using Webview2.Bindings.Models;
using Webview2.Bindings.Models.Abtractions;

namespace Webview2Desktop;

public class Ipc : IIpc
{
    #region Singleton

    private static Ipc _instance;

    public static Ipc Instance => _instance ??= new Ipc();

    #endregion

    #region Entries

    [WebviewInvokable("randomNumber")]
    public Task<OutputMessage> GiveRandomCsharpNumber(InputMessage message)
    {
        Random random = new();
        return Task.FromResult(new OutputMessage(message, random.Next(message.Data!.Value<int>("min"), 100)));
    }

    #endregion
}