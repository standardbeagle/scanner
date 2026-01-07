using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Scanner.Core.Models;

/// <summary>
/// Represents a multi-page scanned document.
/// </summary>
public partial class ScannedDocument : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = "Scanned Document";

    [ObservableProperty]
    private ObservableCollection<ScannedPage> _pages = [];

    [ObservableProperty]
    private DateTime _createdAt = DateTime.UtcNow;

    [ObservableProperty]
    private DateTime _modifiedAt = DateTime.UtcNow;

    /// <summary>
    /// Adds a page to the document.
    /// </summary>
    public void AddPage(ScannedPage page)
    {
        page.PageNumber = Pages.Count + 1;
        Pages.Add(page);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reorders a page from one position to another.
    /// </summary>
    public void ReorderPage(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= Pages.Count) return;
        if (newIndex < 0 || newIndex >= Pages.Count) return;
        if (oldIndex == newIndex) return;

        var page = Pages[oldIndex];
        Pages.RemoveAt(oldIndex);
        Pages.Insert(newIndex, page);

        // Update page numbers
        for (int i = 0; i < Pages.Count; i++)
        {
            Pages[i].PageNumber = i + 1;
        }

        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a page from the document.
    /// </summary>
    public void RemovePage(ScannedPage page)
    {
        if (Pages.Remove(page))
        {
            // Update page numbers
            for (int i = 0; i < Pages.Count; i++)
            {
                Pages[i].PageNumber = i + 1;
            }
            ModifiedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Clears all pages from the document.
    /// </summary>
    public void Clear()
    {
        Pages.Clear();
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the total page count.
    /// </summary>
    public int PageCount => Pages.Count;
}
