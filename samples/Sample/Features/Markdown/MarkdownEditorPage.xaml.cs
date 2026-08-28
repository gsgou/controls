using CommunityToolkit.Mvvm.ComponentModel;
using Shiny;

namespace Sample.Features.Markdown;

public partial class MarkdownEditorPage : ContentPage
{
    public MarkdownEditorPage()
    {
        InitializeComponent();
        SampleSourceCode.Attach(this);
    }
}

[ShellMap<MarkdownEditorPage>(registerRoute: false)]
public partial class MarkdownEditorViewModel : ObservableObject
{
    [ObservableProperty]
    string markdown = """
        # Welcome to the Editor

        Try using the **toolbar** above to format text, or toggle the preview with the eye button.

        On a phone the same toolbar rides the top of the soft keyboard, which is where you
        actually want it - the one above is covered the moment you start typing.

        - Bold, italic, and code formatting
        - Headings (H1, H2, H3)
        - Lists and task lists
        - Links, quotes, and code blocks
        """;

    [ObservableProperty]
    bool showToolbarInKeyboard = true;
}
