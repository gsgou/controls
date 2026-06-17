using System.ComponentModel;

namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// Internal per-row wrapper so the grid can track selection/edit state without mutating the user's
/// data objects. Row cell content binds to <see cref="Data"/>.
/// </summary>
sealed class DataGridRow : INotifyPropertyChanged
{
    bool isSelected;
    bool isEditing;

    public DataGridRow(object data) => this.Data = data;

    public object Data { get; }

    /// <summary>Row position used for striping.</summary>
    public int Index { get; init; }

    public bool IsSelected
    {
        get => this.isSelected;
        set
        {
            if (this.isSelected == value)
                return;
            this.isSelected = value;
            this.PropertyChanged?.Invoke(this, IsSelectedArgs);
        }
    }

    public bool IsEditing
    {
        get => this.isEditing;
        set
        {
            if (this.isEditing == value)
                return;
            this.isEditing = value;
            this.PropertyChanged?.Invoke(this, IsEditingArgs);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    static readonly PropertyChangedEventArgs IsSelectedArgs = new(nameof(IsSelected));
    static readonly PropertyChangedEventArgs IsEditingArgs = new(nameof(IsEditing));
}
