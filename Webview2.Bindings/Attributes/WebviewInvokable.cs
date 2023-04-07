using System;

namespace Webview2.Bindings.Attributes;

[AttributeUsage(validOn: AttributeTargets.Method)]
public class WebviewInvokable : Attribute
{
    #region Props

    public string Name { get; }

    #endregion

    #region Constructors

    public WebviewInvokable(string name)
    {
        Name = name;
    }

    #endregion
}