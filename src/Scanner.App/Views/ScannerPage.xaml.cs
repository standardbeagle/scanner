using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Scanner.Core.Models;
using Scanner.Core.Services;

namespace Scanner.App.Views;

/// <summary>
/// The main scanning page.
/// </summary>
public sealed partial class ScannerPage : Page
{
    public ScannerViewModel ViewModel { get; }

    public ScannerPage()
    {
        ViewModel = App.GetService<ScannerViewModel>();
        this.InitializeComponent();

        // Set default values
        DpiComboBox.SelectedItem = 300;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
    }

    private void ColorModeSegmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ColorModeSegmented.SelectedItem is FrameworkElement element && element.Tag is string tag)
        {
            ViewModel.Settings.ColorMode = tag switch
            {
                "Color" => ColorMode.Color,
                "Grayscale" => ColorMode.Grayscale,
                "BlackAndWhite" => ColorMode.BlackAndWhite,
                _ => ColorMode.Color
            };
        }
    }

    private void PaperSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PaperSizeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.PaperSize = tag switch
            {
                "Letter" => PaperSize.Letter,
                "Legal" => PaperSize.Legal,
                "A4" => PaperSize.A4,
                "A5" => PaperSize.A5,
                _ => PaperSize.Letter
            };
        }
    }
}
