using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// Force the internal Kestrel server to run exclusively on port 8080
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(8080);
});

var app = builder.Build();

// Custom Middleware: Simple console logging for incoming 2019 game client requests
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
            PhotonAppId = "YOUR_PHOTON_APP_ID",
            ApiBaseUrl = $"http://{host}", // Hands off port 8080 routing to subsequent API endpoints
            NotificationHubProvider = "None"
        }
    });
});

app.MapGet("/api/versioncheck/v3", () => Results.Json(new
{
    Valid = true,
    Result = 0, // Bypasses client version update screen
    Message = "Version Accepted"
}));

app.MapPost("/api/auth/v1/loginAccountDevice", () => Results.Json(new
{
    Success = true,
    Token = "csharp_steamless_token",
    PlayerId = 182,
    ScreenName = "RecPlayer"
}));

// --- 2. Player Data Endpoints ---

app.MapGet("/api/accounts/v1/me", () => Results.Json(new
{
    AccountId = 182,
    Username = "RecPlayer",
    DisplayName = "RecPlayer",
    RegistrationStatus = 2,
    IsDeveloper = true
}));

app.MapGet("/api/players/v1/me", () => Results.Json(new
{
    PlayerId = 182,
    Level = 30,
    XP = 9999,
    Platform = 0
}));

// --- 3. Inventory & Item Types System (Types 2-9) ---

app.MapGet("/api/consumables/v1/getBalances", () => Results.Json(new[]
{
    new { CurrencyType = 0, Balance = 50000 }, // Standard Watch Tokens
    new { CurrencyType = 1, Balance = 100 }
}));

app.MapGet("/api/equipment/v1/getUnlocked", () => Results.Json(new[]
{
    new { EquipmentId = "all_unlocked", Type = 2 }, // Torso Items
    new { EquipmentId = "all_unlocked", Type = 3 }, // Headwear
    new { EquipmentId = "all_unlocked", Type = 4 }, // Face / Glasses
    new { EquipmentId = "all_unlocked", Type = 5 }, // Gloves / Hands
    new { EquipmentId = "all_unlocked", Type = 6 }, // Hair Styles
    new { EquipmentId = "all_unlocked", Type = 7 }, // Facial Hair
    new { EquipmentId = "all_unlocked", Type = 8 }, // Weapon Skins
    new { EquipmentId = "all_unlocked", Type = 9 }  // Consumables / Potions
}));

app.MapGet("/api/store/v1/getInventory", () => Results.Json(Array.Empty<object>()));

// --- 4. Room & Profile Customization ---

app.MapGet("/api/rooms/v2/search", () => Results.Json(Array.Empty<object>()));

app.MapGet("/api/rooms/v1/featured", () => Results.Json(new
{
    SubRooms = Array.Empty<object>(),
    Rooms = new[]
    {
        new { RoomId = 1, Name = "DormRoom", DisplayName = "Dorm Room", MaxPlayers = 1, CreatorAccountId = 182 }
    }
}));

app.MapGet("/api/avatar/v2", () => Results.Json(new
{
    AccountId = 182,
    AvatarItems = new[]
    {
        new { Type = 2, ItemName = "it_torso_hoodie" },
        new { Type = 6, ItemName = "it_hair_messy" }
    },
    FaceFeatures = Array.Empty<object>()
}));

app.MapPost("/api/avatar/v2/update", async (HttpContext context) =>
{
    // Simply acknowledgment so the client wardrobe mirror updates smoothly without hanging
    Console.WriteLine("--> Mirror updated player cosmetics.");
    return Results.Json(new { Success = true });
});

Console.WriteLine("🚀 Barebones 2019 C# Server running on http://localhost:8080");
app.Run();
