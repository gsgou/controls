// One namespace for everything a XAML author needs, so pages can declare a single prefix:
//   xmlns:kf="http://shiny.net/maui/keyframe"
[assembly: Microsoft.Maui.Controls.XmlnsDefinition("http://shiny.net/maui/keyframe", "Shiny.Maui.Controls.Keyframe")]
[assembly: Microsoft.Maui.Controls.XmlnsDefinition("http://shiny.net/maui/keyframe", "Shiny.Controls.Keyframe", AssemblyName = "Shiny.Controls.Keyframe.Shared")]
[assembly: Microsoft.Maui.Controls.XmlnsDefinition("http://shiny.net/maui/keyframe", "Shiny.Controls.Keyframe.Graphics", AssemblyName = "Shiny.Controls.Keyframe.Shared")]
[assembly: Microsoft.Maui.Controls.XmlnsPrefix("http://shiny.net/maui/keyframe", "kf")]
