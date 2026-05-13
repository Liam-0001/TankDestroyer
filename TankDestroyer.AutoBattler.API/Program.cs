using System.Text.Json;
using TankDestroyer.AutoBattler.API.Extensions;
using TankDestroyer.AutoBattler.API.Hubs;
using TankDestroyer.AutoBattler.API.Services;
using TankDestroyer.Engine.Services.Instantiate;

var builder = WebApplication.CreateBuilder(args);

var botFolder = ResolvePath("Bots"); 
var mapFolder = ResolvePath("Maps");

var botTypes = new CollectBotsService().LoadBots(botFolder);
var maps = new CollectMapsService().LoadMaps(mapFolder);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

builder.Services.AddHostedService<BattleResultService>();
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

static string ResolvePath(string folderName)
{
    // De map waar de .exe staat (D:\Desktop\bot\Build\)
    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
    
    // We kijken eerst direct in de build map
    var path = Path.Combine(baseDir, folderName);

    // Als hij daar niet staat (bijv. tijdens debuggen vanuit de IDE), 
    // kijken we een niveau hoger in de root van je project.
    if (!Directory.Exists(path))
    {
        path = Path.GetFullPath(Path.Combine(baseDir, "..", folderName));
    }

    if (!Directory.Exists(path))
    {
        throw new DirectoryNotFoundException(
            $"Kritieke fout: Map '{folderName}' niet gevonden.\n" +
            $"Gezocht in: {Path.Combine(baseDir, folderName)}\n" +
            $"En in: {path}");
    }

    Console.WriteLine($"[Config] {folderName} geladen uit: {path}");
    return path;
}