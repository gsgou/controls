namespace Shiny.Blazor.Controls.OnScreenKeyboard;

enum OnScreenKeyKind
{
    Character,
    Space,
    Backspace,
    Enter,
    Tab,
    Shift,
    CapsLock,
    Layer,
    Arrow,
    Hide
}

/// <summary>One key. Immutable and layer-independent — shift and caps are resolved at render time.</summary>
sealed class OnScreenKey
{
    public required OnScreenKeyKind Kind { get; init; }

    /// <summary>The unshifted character, or the arrow direction for <see cref="OnScreenKeyKind.Arrow"/>.</summary>
    public string Value { get; init; } = "";

    /// <summary>What the key types while Shift is held. Null for keys that do not shift.</summary>
    public string? ShiftValue { get; init; }

    /// <summary>The face of a non-character key.</summary>
    public string? Glyph { get; init; }

    /// <summary>Share of the row's width, relative to a plain letter key.</summary>
    public double Width { get; init; } = 1;

    public bool Repeats { get; init; }

    /// <summary>Spoken name. Glyph-faced keys need one; a letter key does not.</summary>
    public string? AriaLabel { get; init; }

    /// <summary>Modifiers and commands paint darker than the letters, the way every OS keyboard does.</summary>
    public bool IsAccent => this.Kind is not (OnScreenKeyKind.Character or OnScreenKeyKind.Space);
}

/// <summary>
/// US-QWERTY in two layers. Shift and Caps Lock are not layers — they are state applied to the
/// letter layer at render time, which is the only way Caps Lock can raise the letters without also
/// shifting the number row, the way a real keyboard behaves.
/// </summary>
static class OnScreenKeyboardLayout
{
    public static readonly IReadOnlyList<IReadOnlyList<OnScreenKey>> Letters =
    [
        [
            Sym("`", "~"), Sym("1", "!"), Sym("2", "@"), Sym("3", "#"), Sym("4", "$"), Sym("5", "%"),
            Sym("6", "^"), Sym("7", "&"), Sym("8", "*"), Sym("9", "("), Sym("0", ")"),
            Sym("-", "_"), Sym("=", "+"), Backspace(2)
        ],
        [
            Tab(1.5),
            Letter("q"), Letter("w"), Letter("e"), Letter("r"), Letter("t"),
            Letter("y"), Letter("u"), Letter("i"), Letter("o"), Letter("p"),
            Sym("[", "{"), Sym("]", "}"), Sym("\\", "|", 1.5)
        ],
        [
            Caps(1.75),
            Letter("a"), Letter("s"), Letter("d"), Letter("f"), Letter("g"),
            Letter("h"), Letter("j"), Letter("k"), Letter("l"),
            Sym(";", ":"), Sym("'", "\""), Enter(2.25)
        ],
        [
            Shift(2.25),
            Letter("z"), Letter("x"), Letter("c"), Letter("v"),
            Letter("b"), Letter("n"), Letter("m"),
            Sym(",", "<"), Sym(".", ">"), Sym("/", "?"), Shift(2.75)
        ]
    ];

    public static readonly IReadOnlyList<IReadOnlyList<OnScreenKey>> Symbols =
    [
        [
            Sym("1"), Sym("2"), Sym("3"), Sym("4"), Sym("5"), Sym("6"), Sym("7"),
            Sym("8"), Sym("9"), Sym("0"), Sym("-"), Sym("="), Sym("+"), Backspace(2)
        ],
        [
            Sym("~"), Sym("!"), Sym("@"), Sym("#"), Sym("$"), Sym("%"), Sym("^"), Sym("&"),
            Sym("*"), Sym("("), Sym(")"), Sym("_"), Sym("["), Sym("]"), Sym("\\")
        ],
        [
            Sym("€"), Sym("£"), Sym("¥"), Sym("¢"), Sym("°"), Sym("±"), Sym("×"), Sym("÷"),
            Sym("{"), Sym("}"), Sym("|"), Sym(":"), Sym(";"), Enter(2)
        ],
        [
            Sym("«"), Sym("»"), Sym("\""), Sym("'"), Sym("<"), Sym(">"), Sym("?"), Sym("/"),
            Sym(","), Sym("."), Sym("•"), Sym("–"), Sym("—"), Sym("…"), Sym("¡")
        ]
    ];

    /// <summary>Shared by both layers — only the layer key's own face changes.</summary>
    public static readonly IReadOnlyList<OnScreenKey> BottomRow =
    [
        new() { Kind = OnScreenKeyKind.Layer, Width = 1.5, AriaLabel = "Switch layer" },
        Sym(","),
        new() { Kind = OnScreenKeyKind.Space, Value = " ", Width = 6, Repeats = true, AriaLabel = "Space" },
        Sym("."),
        Arrow("left", "◀", "Move caret left"),
        Arrow("down", "▼", "Move caret down"),
        Arrow("up", "▲", "Move caret up"),
        Arrow("right", "▶", "Move caret right"),
        new() { Kind = OnScreenKeyKind.Hide, Glyph = "⌄", Width = 1.5, AriaLabel = "Hide keyboard" }
    ];

    static OnScreenKey Letter(string value) => new()
    {
        Kind = OnScreenKeyKind.Character,
        Value = value,
        ShiftValue = value.ToUpperInvariant(),
        Repeats = true
    };

    static OnScreenKey Sym(string value, string? shift = null, double width = 1) => new()
    {
        Kind = OnScreenKeyKind.Character,
        Value = value,
        ShiftValue = shift,
        Width = width,
        Repeats = true
    };

    static OnScreenKey Arrow(string direction, string glyph, string aria) => new()
    {
        Kind = OnScreenKeyKind.Arrow,
        Value = direction,
        Glyph = glyph,
        Repeats = true,
        AriaLabel = aria
    };

    static OnScreenKey Backspace(double width) => new()
        { Kind = OnScreenKeyKind.Backspace, Glyph = "⌫", Width = width, Repeats = true, AriaLabel = "Backspace" };

    static OnScreenKey Enter(double width) => new()
        { Kind = OnScreenKeyKind.Enter, Glyph = "⏎", Width = width, AriaLabel = "Enter" };

    static OnScreenKey Tab(double width) => new()
        { Kind = OnScreenKeyKind.Tab, Glyph = "⇥", Width = width, AriaLabel = "Tab" };

    static OnScreenKey Shift(double width) => new()
        { Kind = OnScreenKeyKind.Shift, Glyph = "⇧", Width = width, AriaLabel = "Shift" };

    static OnScreenKey Caps(double width) => new()
        { Kind = OnScreenKeyKind.CapsLock, Glyph = "⇪", Width = width, AriaLabel = "Caps lock" };
}
