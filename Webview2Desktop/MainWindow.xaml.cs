using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Webview2.Bindings;
using Webview2.Bindings.Models;

namespace Webview2Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        _ = Bootstrapper.Instance.Bootstrap(
            WebView,
            Ipc.Instance,
            new BootstrapOptions("Webview2Desktop", "http://localhost:5173")
        );

        Task.Run(() =>
        {
            // Send a random number to webview every second
            Random random = new();
            while (true)
            {
                Thread.Sleep(1000);
                Bootstrapper.Instance.PostMessage(new OutputMessage("randomNumber2", random.Next(0, 100)));
            }
        });
    }
}