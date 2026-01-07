using Microsoft.UI.Xaml.Controls;
using Scanner.Core.Services;

namespace Scanner.App.Views;

/// <summary>
/// Page for managing multi-page documents.
/// </summary>
public sealed partial class DocumentPage : Page
{
    public DocumentViewModel ViewModel { get; }

    public DocumentPage()
    {
        ViewModel = App.GetService<DocumentViewModel>();
        this.InitializeComponent();
    }
}
