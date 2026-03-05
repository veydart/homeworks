using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SocialNetwork.Application.Interfaces;
using SocialNetwork.Application.Services;
using SocialNetwork.Domain.Interfaces;
using SocialNetwork.Infrastructure.Cache;
using SocialNetwork.Infrastructure.Data;
using SocialNetwork.Infrastructure.Messaging;
using SocialNetwork.Infrastructure.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL + EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=social_network;Username=postgres;Password=root";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Redis
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

// RabbitMQ
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
builder.Services.AddSingleton<IPostEventPublisher>(sp =>
    RabbitMqPublisher.CreateAsync(rabbitHost).GetAwaiter().GetResult());

// WebSocket
builder.Services.AddSingleton<WebSocketConnectionManager>();

// Репозитории
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IFriendRepository, FriendRepository>();
builder.Services.AddScoped<IDialogRepository, DialogRepository>();

// Сервисы
builder.Services.AddScoped<IFeedCacheService, RedisFeedCacheService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<FeedService>();
builder.Services.AddScoped<DialogService>();

// FeedConsumer (фоновый обработчик очереди)
builder.Services.AddHostedService(sp =>
    new FeedConsumer(
        sp.GetRequiredService<IServiceScopeFactory>(),
        sp.GetRequiredService<WebSocketConnectionManager>(),
        sp.GetRequiredService<ILogger<FeedConsumer>>(),
        rabbitHost));

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForSocialNetwork2024!@#$";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SocialNetwork",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SocialNetwork",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Автоматическая миграция БД
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

// WebSocket endpoint
app.UseWebSockets();
app.Map("/post/feed/posted", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    // Аутентификация через query-параметр token
    var token = context.Request.Query["token"].ToString();
    Guid userId;
    try
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = app.Configuration["Jwt:Issuer"] ?? "SocialNetwork",
            ValidAudience = app.Configuration["Jwt:Audience"] ?? "SocialNetwork",
            IssuerSigningKey = key
        }, out _);
        userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
    catch
    {
        context.Response.StatusCode = 401;
        return;
    }

    var wsManager = context.RequestServices.GetRequiredService<WebSocketConnectionManager>();
    using var ws = await context.WebSockets.AcceptWebSocketAsync();

    wsManager.AddConnection(userId, ws);

    var buffer = new byte[1024];
    try
    {
        while (ws.State == WebSocketState.Open)
        {
            await ws.ReceiveAsync(buffer, CancellationToken.None);
        }
    }
    catch { }
    finally
    {
        wsManager.RemoveConnection(userId, ws);
    }
});

app.MapControllers();

app.Run();
