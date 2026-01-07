using Microsoft.UI.Xaml.Controls;
using Scanner.Core.Services;

namespace Scanner.App.Views;

/// <summary>
/// Page for exporting documents.
/// </summary>
public sealed partial class ExportPage : Page
{
    public ExportViewModel ViewModel { get; }

    public ExportPage()
    {
        ViewModel = App.GetService<ExportViewModel>();
        this.InitializeComponent();
    }
}
