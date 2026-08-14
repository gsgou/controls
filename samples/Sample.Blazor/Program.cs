using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sample.Blazor;
using Shiny.Blazor.Controls;
using Shiny.Blazor.Controls.Dialogs;
using Shiny.Blazor.Controls.Docking;
using Shiny.Blazor.Controls.Splash;
using Shiny.Blazor.Controls.Toast;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddShinyToast();
builder.Services.AddShinySplashScreen();
builder.Services.AddShinyDialogs();
builder.Services.AddShinyDocking();
builder.Services.AddShinyWalkthrough();
builder.Services.AddDockPanel<Sample.Blazor.DockPanels.ExplorerPanel>("explorer", "Explorer", "📁");
builder.Services.AddDockPanel<Sample.Blazor.DockPanels.PropertiesPanel>("properties", "Properties", "🔧");
builder.Services.AddDockPanel<Sample.Blazor.DockPanels.OutputPanel>("output", "Output", "🖥️");
builder.Services.AddDockPanel<Sample.Blazor.DockPanels.ErrorListPanel>("errors", "Error List", "⚠️");
builder.Services.AddDockPanel<Sample.Blazor.DockPanels.EditorPanel>("editor", "Program.cs", "📄");
builder.Services.AddDockPanel<Sample.Blazor.DockPanels.ReadmePanel>("readme", "README.md", "📘");
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<Sample.Blazor.Chat.InMemoryChatSessionProvider>();
builder.Services.AddSingleton<Sample.Blazor.Chat.KitchenSinkChatProvider>();

await builder.Build().RunAsync();
