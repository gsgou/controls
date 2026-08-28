// The ribbon is surfaced under the same namespace URI as the core controls, so a XAML author writes
// `shiny:Ribbon` beside `shiny:TextEntry` and never has to know there are two assemblies involved.
// Docking, the tray icon and quick entry's hotkeys are API rather than markup and are not mapped.
[assembly: Microsoft.Maui.Controls.XmlnsDefinition(
    "http://shiny.net/maui/controls",
    "Shiny.Maui.Controls.Desktop.Ribbons"
)]
