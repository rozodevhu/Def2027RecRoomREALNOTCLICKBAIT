using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(8080);
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine($"[{context.Request.Method}] {context.Request.Path}");
    await next();
});

// --- 1. Handshake & Config Endpoints ---

app.MapGet("/api/config/v2", (HttpContext context) =>
{
    var host = context.Request.Host.Value;
    return Results.Json(new
    {
        Valid = true,
        Message = "Server online",
        Data = new
        {
            MatchmakingProvider = "Photon",
            // To test multiplayer with friends, replace this with a valid Photon Realtime App ID.
            // Leaving it as a dummy string will still allow local/solo room loading.
            PhotonAppId = "YOUR_PHOTON_REALTIME_APP_ID", 
            ApiBaseUrl = $"http://{host}",
            NotificationHubProvider = "None"
        }
    });
});

app.MapGet("/api/versioncheck/v3", () => Results.Json(new { Valid = true, Result = 0, Message = "Version Accepted" }));

app.MapPost("/api/auth/v1/loginAccountDevice", () => Results.Json(new
{
    Success = true,
    Token = "csharp_steamless_token",
    PlayerId = 182,
    ScreenName = "RecPlayer"
}));

// --- 2. Player Data Endpoints ---

app.MapGet("/api/accounts/v1/me", () => Results.Json(new { AccountId = 182, Username = "RecPlayer", DisplayName = "RecPlayer", RegistrationStatus = 2, IsDeveloper = true }));
app.MapGet("/api/players/v1/me", () => Results.Json(new { PlayerId = 182, Level = 30, XP = 9999, Platform = 0 }));

// --- 3. Inventory & Item Types (2-9) ---

app.MapGet("/api/consumables/v1/getBalances", () => Results.Json(new[] { new { CurrencyType = 0, Balance = 50000 }, new { CurrencyType = 1, Balance = 100 } }));
app.MapGet("/api/equipment/v1/getUnlocked", () => Results.Json(new[]
{
    new { EquipmentId = "all_unlocked", Type = 2 }, new { EquipmentId = "all_unlocked", Type = 3 },
    new { EquipmentId = "all_unlocked", Type = 4 }, new { EquipmentId = "all_unlocked", Type = 5 },
    new { EquipmentId = "all_unlocked", Type = 6 }, new { EquipmentId = "all_unlocked", Type = 7 },
    new { EquipmentId = "all_unlocked", Type = 8 }, new { EquipmentId = "all_unlocked", Type = 9 }
}));
app.MapGet("/api/store/v1/getInventory", () => Results.Json(Array.Empty<object>()));

// --- 4. Room & Profile Customization ---

app.MapGet("/api/avatar/v2", () => Results.Json(new
{
    AccountId = 182,
    AvatarItems = new[] { new { Type = 2, ItemName = "it_torso_hoodie" }, new { Type = 6, ItemName = "it_hair_messy" } },
    FaceFeatures = Array.Empty<object>()
}));

app.MapPost("/api/avatar/v2/update", () => Results.Json(new { Success = true }));

// --- 5. COMPREHENSIVE ROOM JOINING & MATCHMAKING SYSTEM ---

// Mock Database of standard 2019 game scenes to handle map names gracefully
var roomDatabase = new Dictionary<string, (int id, string displayName, string sceneName)>
{
    { "dormroom", (1, "Dorm Room", "DormRoom") },
    { "paintball", (2, "Paintball", "Paintball") },
    { "cyberjunkcity", (3, "Cyberjunk City", "CyberJunkCity") },
    { "clearcut", (4, "Clearcut", "ClearCut") },
    { "quarry", (5, "Quarry", "Quarry") },
    { "goldentrophy", (6, "Quest For The Golden Trophy", "Quest_GoldenTrophy") },
    { "jriv", (7, "Jumbotron", "Quest_Jumbotron") },
    { "dracula", (8, "Crescendo of the Blood Moon", "Quest_Dracula") },
    { "pirates", (9, "Isle of Lost Skulls", "Quest_Pirates") },
    { "lasertag", (10, "Laser Tag", "LaserTag") },
    { "recroom", (11, "The Rec Center", "RecCenter") },
    { "lounge", (12, "The Lounge", "Lounge") },
    { "park", (13, "The Park", "Park") },
    { "charades", (14, "3D Charades", "Charades") },
    { "bowling", (15, "Bowling", "Bowling") },
    { "soccer", (16, "Rec Room Soccer", "Soccer") }
};

// Returns a single room by its string code name
app.MapGet("/api/rooms/v2/byName/{roomName}", (string roomName) =>
{
    var key = roomName.ToLower();
    var (id, disp, scene) = roomDatabase.ContainsKey(key) ? roomDatabase[key] : (999, roomName, roomName);

    return Results.Json(new
    {
        RoomId = id,
        Name = roomName,
        DisplayName = disp,
        Description = "An emulated 2019 private room.",
        MaxPlayers = 20,
        CreatorAccountId = 182,
        State = 1,
        Version = 1,
        IsFeatured = true,
        SubRooms = new[]
        {
            new { SubRoomId = id * 10, Name = "MainInstance", RoomId = id, UnitySceneId = scene }
        }
    });
});

// Returns bulk details for the loading screens when joining a room
app.MapPost("/api/rooms/v2/getDetails", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    
    // Fallback response structures so the watch screen populates properly
    return Results.Json(Array.Empty<object>());
});

// Search and featured rooms catalog mappings
app.MapGet("/api/rooms/v2/search", () => Results.Json(Array.Empty<object>()));
app.MapGet("/api/rooms/v1/featured", () => Results.Json(new
{
    SubRooms = Array.Empty<object>(),
    Rooms = new[] { new { RoomId = 1, Name = "DormRoom", DisplayName = "Dorm Room", MaxPlayers = 1, CreatorAccountId = 182 } }
}));

// Matchmaking v4 system - intercepting game client instance searches
app.MapPost("/api/matchmaking/v4/search", () =>
{
    // Return an empty list to tell the client: "No lobbies exist yet, create a new one"
    return Results.Json(Array.Empty<object>());
});

// Matchmaking v4 Room Creation/Session Registration Hook
app.MapPost("/api/matchmaking/v4/create", (HttpContext context) =>
{
    // Generates a mock game room instance configuration
    return Results.Json(new
    {
        Result = 0, // 0 = Success
        RoomInstance = new
        {
            RoomInstanceId = new Random().Next(10000, 99999),
            RoomId = 1,
            SubRoomId = 10,
            NameCode = "EMU-PRIVATE-ROOM",
            PhotonRegion = "USW",
            PhotonRoomName = $"EmuLobby_{Guid.NewGuid().ToString().Substring(0, 8)}",
            MaxPlayers = 10,
            PlayerCount = 1
        }
    });
});

// Fallback map endpoint for tracking active player sessions
app.MapPost("/api/matchmaking/v4/join", () => Results.Json(new { Result = 0 }));

Console.WriteLine("🚀 Upgraded 2019 Room-Ready Server active on http://localhost:8080");
app.Run();
