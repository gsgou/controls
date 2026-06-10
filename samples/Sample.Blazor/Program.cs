using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sample.Blazor;
using Shiny.Blazor.Controls.Kiosk.Docking;
using Shiny.Blazor.Controls.Toast;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddShinyToast();
builder.Services.AddShinyDocking();
builder.Services.AddDockPanel<Sample.Blazor.DockPanels.ExplorerPanel>("explorer", "Explorer");
builder.Services.AddDockPanel<Sample.Blazor.DockPanels.PropertiesPanel>("properties", "Properties");
builder.Services.AddDockPanel<Sample.Blazor.DockPanels.OutputPanel>("output", "Output");
builder.Services.AddDockPanel<Sample.Blazor.DockPanels.ErrorListPanel>("errors", "Error List");
builder.Services.AddDockPanel<Sample.Blazor.DockPanels.EditorPanel>("editor", "Program.cs");
builder.Services.AddDockPanel<Sample.Blazor.DockPanels.ReadmePanel>("readme", "README.md");
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
