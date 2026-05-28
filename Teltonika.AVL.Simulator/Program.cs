using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace Teltonika.AVL.Simulator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register the Singleton SimulatorManager
        var simManager = new SimulatorManager();
        
        // Read configuration settings from environment variables injected by Aspire
        simManager.TargetHost = Environment.GetEnvironmentVariable("TELTONIKA_HOST") ?? "127.0.0.1";
        var portStr = Environment.GetEnvironmentVariable("TELTONIKA_PORT");
        if (int.TryParse(portStr, out var port))
        {
            simManager.TargetPort = port;
        }

        builder.Services.AddSingleton(simManager);
        
        // CORS config for dashboard access
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        var app = builder.Build();

        app.UseCors();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Minimal APIs for client front-end integration
        app.MapGet("/api/trackers", (SimulatorManager manager) => Results.Ok(manager.GetTrackers()));
        
        app.MapGet("/api/trackers/{imei}/logs", (string imei, SimulatorManager manager) => 
            Results.Ok(manager.GetLogs(imei)));

        app.MapPost("/api/trackers", (CreateTrackerRequest req, SimulatorManager manager) =>
        {
            if (string.IsNullOrWhiteSpace(req.Imei) || req.Imei.Length < 5)
            {
                return Results.BadRequest("Invalid IMEI length.");
            }
            manager.AddTracker(req.Imei, req.Instructions, req.Latitude, req.Longitude);
            return Results.Ok();
        });

        app.MapDelete("/api/trackers/{imei}", (string imei, SimulatorManager manager) =>
        {
            manager.RemoveTracker(imei);
            return Results.Ok();
        });

        app.MapPost("/api/trackers/{imei}/start", (string imei, SimulatorManager manager) =>
        {
            manager.StartTracker(imei);
            return Results.Ok();
        });

        app.MapPost("/api/trackers/{imei}/stop", (string imei, SimulatorManager manager) =>
        {
            manager.StopTracker(imei);
            return Results.Ok();
        });

        app.MapPost("/api/trackers/{imei}/update", (UpdateTrackerRequest req, string imei, SimulatorManager manager) =>
        {
            manager.UpdateTracker(imei, req.Instructions);
            return Results.Ok();
        });

        app.Run();
    }
}

public class CreateTrackerRequest
{
    public string Imei { get; set; } = null!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<TrackerInstruction> Instructions { get; set; } = new();
}

public class UpdateTrackerRequest
{
    public List<TrackerInstruction> Instructions { get; set; } = new();
}