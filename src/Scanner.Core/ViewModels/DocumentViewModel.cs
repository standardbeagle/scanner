using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scanner.Core.Models;

namespace Scanner.Core.Services;

/// <summary>
/// ViewModel for multi-page document management.
/// </summary>
public partial class DocumentViewModel : ObservableObject
{
    [ObservableProperty]
    private ScannedDocument _document = new();

    [ObservableProperty]
    private ScannedPage? _selectedPage;

    [ObservableProperty]
    private bool _isDragging;

    [ObservableProperty]
    private int _selectedPageIndex = -1;

    /// <summary>
    /// Sets the document to manage.
    /// </summary>
    public void SetDocument(ScannedDocument document)
    {
        Document = document;
        SelectedPage = document.Pages.FirstOrDefault();
    }

    [RelayCommand]
    private void SelectPage(ScannedPage page)
    {
        SelectedPage = page;
        SelectedPageIndex = Document.Pages.IndexOf(page);
    }

    public void ReorderPages(int fromIndex, int toIndex)
    {
        Document.ReorderPage(fromIndex, toIndex);
    }

    [RelayCommand]
    private void DeletePage(ScannedPage page)
    {
        var wasSelected = SelectedPage == page;
        var index = Document.Pages.IndexOf(page);

        Document.RemovePage(page);

        if (wasSelected && Document.Pages.Count > 0)
        {
            // Select the nearest page
            var newIndex = Math.Min(index, Document.Pages.Count - 1);
            SelectedPage = Document.Pages[newIndex];
        }
        else if (Document.Pages.Count == 0)
        {
            SelectedPage = null;
        }
    }

    [RelayCommand]
    private void RotatePage(ScannedPage page)
    {
        page.Rotation = (page.Rotation + 90) % 360;
    }

    [RelayCommand]
    private void RotatePageCounterClockwise(ScannedPage page)
    {
        page.Rotation = (page.Rotation + 270) % 360;
    }

    [RelayCommand]
    private void MovePageUp(ScannedPage page)
    {
        var index = Document.Pages.IndexOf(page);
        if (index > 0)
        {
            Document.ReorderPage(index, index - 1);
        }
    }

    [RelayCommand]
    private void MovePageDown(ScannedPage page)
    {
        var index = Document.Pages.IndexOf(page);
        if (index < Document.Pages.Count - 1)
        {
            Document.ReorderPage(index, index + 1);
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        // For potential future multi-select functionality
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedPage != null)
        {
            DeletePage(SelectedPage);
        }
    }

    partial void OnSelectedPageChanged(ScannedPage? value)
    {
        if (value != null)
        {
            SelectedPageIndex = Document.Pages.IndexOf(value);
        }
        else
        {
            SelectedPageIndex = -1;
        }
    }
}
