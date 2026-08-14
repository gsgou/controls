using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sample.Blazor;
using Sample.Blazor.DockPanels;
using Shiny.Blazor.Controls;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// One call covers every service-backed control - Toast, Dialogs, the splash screen, the walkthrough
// store, docking and the on-screen keyboard. The individual AddShiny* calls still exist for apps
// that want to keep the WASM payload tight; dock panels can only ever come from the app.
builder.Services.AddShinyControls(cfg => cfg
    .AddDockPanel<ExplorerPanel>("explorer", "Explorer", "📁")
    .AddDockPanel<PropertiesPanel>("properties", "Properties", "🔧")
    .AddDockPanel<OutputPanel>("output", "Output", "🖥️")
    .AddDockPanel<ErrorListPanel>("errors", "Error List", "⚠️")
    .AddDockPanel<EditorPanel>("editor", "Program.cs", "📄")
    .AddDockPanel<ReadmePanel>("readme", "README.md", "📘")
);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<Sample.Blazor.Chat.InMemoryChatSessionProvider>();
builder.Services.AddSingleton<Sample.Blazor.Chat.KitchenSinkChatProvider>();

await builder.Build().RunAsync();
