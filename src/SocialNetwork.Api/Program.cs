using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SocialNetwork.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=social_network;Username=postgres;Password=root";

var dataSource = NpgsqlDataSource.Create(connectionString);
builder.Services.AddSingleton(dataSource);
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<PasswordHasher>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

await InitializeDatabase(dataSource, connectionString);

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

async Task InitializeDatabase(NpgsqlDataSource ds, string connString)
{
    var csb = new NpgsqlConnectionStringBuilder(connString);
    var dbName = csb.Database;
    csb.Database = "postgres";

    await using var adminDs = NpgsqlDataSource.Create(csb.ToString());
    await using var checkCmd = adminDs.CreateCommand(
        $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'");
    var exists = await checkCmd.ExecuteScalarAsync();

    if (exists is null)
    {
        await using var createCmd = adminDs.CreateCommand($"CREATE DATABASE \"{dbName}\"");
        await createCmd.ExecuteNonQueryAsync();
    }

    var sqlPath = Path.Combine(AppContext.BaseDirectory, "Database", "init.sql");
    if (!File.Exists(sqlPath))
        sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "init.sql");

    if (File.Exists(sqlPath))
    {
        var sql = await File.ReadAllTextAsync(sqlPath);
        await using var cmd = ds.CreateCommand(sql);
        await cmd.ExecuteNonQueryAsync();
    }
}
