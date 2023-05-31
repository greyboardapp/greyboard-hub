using Greyboard.Configuration;
using Greyboard.Core;
using Greyboard.Core.Managers;
using Greyboard.Hubs;
using Greyboard.Managers;
using Greyboard.Services;

var CORS_NAME = "ClientPermission";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => { });

builder.Services.AddSingleton<AppSettings>();
builder.Services.AddSingleton<IClientManager, ClientManager>();
builder.Services.AddSingleton<IBoardManager, BoardManager>();

builder.Services.AddHostedService<HeartBeatService>();

builder.Services.AddHttpClient();
builder.Services.AddSignalR();

builder.Services.AddCorsConfiguration(CORS_NAME);

var app = builder.Build();

app.UseCors(CORS_NAME);
app.UseHttpsRedirection();

app.UseRouting();
app.MapHub<BoardHub>("/boards");

app.Run();
