namespace Shiny.Blazor.Controls;

/// <summary>Which way a <see cref="Slider"/> runs.</summary>
public enum SliderOrientation
{
    /// <summary>Minimum on the left, maximum on the right.</summary>
    Horizontal,

    /// <summary>Minimum at the bottom, maximum at the top — the direction a level or a fader reads.</summary>
    Vertical
}


/// <summary>How a <see cref="SliderMark"/> draws itself on the track.</summary>
public enum SliderMarkShape
{
    /// <summary>A small filled circle sitting on the track, with <see cref="SliderMark.Text"/> as a caption beside it.</summary>
    Dot,

    /// <summary>
    /// A rounded badge centred on the track with <see cref="SliderMark.Text"/> inside it. Use it when the
    /// label is the mark — a handful of named stops rather than a dense scale.
    /// </summary>
    Bubble,

    /// <summary>A thin tick drawn across the track, with <see cref="SliderMark.Text"/> as a caption beside it.</summary>
    Line
}
