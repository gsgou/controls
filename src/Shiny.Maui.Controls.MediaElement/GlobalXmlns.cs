[assembly: Microsoft.Maui.Controls.XmlnsDefinition("http://shiny.net/maui/media", "Shiny.Maui.Controls.Media")]
// Surface the shared types (MediaElementState, MediaAspect, MediaMetadata, MediaPlaybackCapabilities) under
// the same xmlns so they're usable in XAML as media:* without a separate clr-namespace declaration.
[assembly: Microsoft.Maui.Controls.XmlnsDefinition("http://shiny.net/maui/media", "Shiny.Controls.Media", AssemblyName = "Shiny.Controls.MediaElement.Shared")]
[assembly: Microsoft.Maui.Controls.XmlnsPrefix("http://shiny.net/maui/media", "media")]
