using Microsoft.UI.Xaml.Controls;
using Scanner.Core.Services;

namespace Scanner.App.Views;

/// <summary>
/// Application settings page.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        this.InitializeComponent();
    }
}
