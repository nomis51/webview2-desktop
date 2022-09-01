using System;
using System.Windows;
using System.Windows.Controls;
using Webview2Desktop.Models;
using Webview2Desktop.ViewModels;
using Webview2Desktop.WebView;

namespace Webview2Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly AppViewModel _viewModel;


    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new AppViewModel();
        _viewModel.OnMessageEvent += ViewModel_OnMessageEvent;

        Loaded += OnLoaded;

        _ = WebViewBootstrapper.Bootstrap(WebView);
    }

    private void ViewModel_OnMessageEvent(string message)
    {
        Border.Visibility = Visibility.Visible;
        TextBlockMessageReceived.Text = $"Message from Angular : {message}";
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        WebViewBootstrapper.PostMessageToWebContent(new WebViewMessage("initialized", true));
    }

    private void ButtonTalkToAngular_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel?.TalkToAngular(TextBoxMessage.Text);
    }

    private void TextBoxMessage_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ButtonTalkToAngular.IsEnabled = !string.IsNullOrEmpty(TextBoxMessage.Text);
    }
}