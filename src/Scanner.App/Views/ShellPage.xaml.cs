using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Scanner.App.Views;

/// <summary>
/// The main shell page with navigation.
/// </summary>
public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        this.InitializeComponent();

        // Set up custom title bar
        if (App.Current.MainWindow != null)
        {
            App.Current.MainWindow.SetTitleBar(AppTitleBar);
        }
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigate to the scanner page by default
        NavView.SelectedItem = NavView.MenuItems[0];
        ContentFrame.Navigate(typeof(ScannerPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            var pageType = tag switch
            {
                "ScannerPage" => typeof(ScannerPage),
                "DocumentPage" => typeof(DocumentPage),
                "ExportPage" => typeof(ExportPage),
                "HelpPage" => typeof(HelpPage),
                _ => typeof(ScannerPage)
            };

            if (ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }
}
