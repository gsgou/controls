using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Sample.Features.Tabs;

public class TabsDemoViewModel : INotifyPropertyChanged
{
    string status = "Tap a tab, or the centre button.";

    public string Status
    {
        get => this.status;
        set
        {
            this.status = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Status)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
