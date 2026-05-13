using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TankDestroyer.AutoBattler;
using TankDestroyer.AutoBattler.Console;
using TankDestroyer.AutoBattler.Console.Extensions;
using TankDestroyer.Engine.Services.Instantiate;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddLoadConfiguration();
    services.AddTransient<IApp,App>();
});

var host = builder.Build();

await host.Services
    .GetRequiredService<IApp>()
    .RunAsync();