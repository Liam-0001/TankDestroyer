using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using TankDestroyer.AutoBattler.API.Extensions;
using TankDestroyer.AutoBattler.API.Hubs;
using TankDestroyer.Engine.Services.Instantiate;

var builder = WebApplication.CreateBuilder(args);

var botFolder = ResolvePath("..\\Build\\Bots", "..\\Bots");
var mapFolder = ResolvePath("..\\Maps", "..\\Maps");

var botTypes = new CollectBotsService().LoadBots(botFolder);
var maps = new CollectMapsService().LoadMaps(mapFolder);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddBattleInfrastructure(botTypes, maps);
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p =>
        p.WithOrigins("http://localhost:63342")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));
var app = builder.Build();
app.UseCors();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<BattleHub>("/battlehub");

app.Run();
return;

static string ResolvePath(string? configuredPath, string fallback)
{
    var value = string.IsNullOrWhiteSpace(configuredPath) ? fallback : configuredPath;
    value = value.Replace('\\', Path.DirectorySeparatorChar);
    return Path.IsPathRooted(value) ? value : Path.GetFullPath(value);
}