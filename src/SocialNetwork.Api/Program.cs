using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SocialNetwork.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var masterConn = builder.Configuration.GetConnectionString("Master")
    ?? "Host=localhost;Port=5432;Database=social_network;Username=postgres;Password=root";

var slave1Conn = builder.Configuration.GetConnectionString("Slave1") ?? "";
var slave2Conn = builder.Configuration.GetConnectionString("Slave2") ?? "";

var masterDataSource = NpgsqlDataSource.Create(masterConn);
builder.Services.AddKeyedSingleton("master", masterDataSource);

var slaveConnections = new List<string>();
if (!string.IsNullOrEmpty(slave1Conn)) slaveConnections.Add(slave1Conn);
if (!string.IsNullOrEmpty(slave2Conn)) slaveConnections.Add(slave2Conn);

if (slaveConnections.Count > 0)
{
    var slaveSources = slaveConnections.Select(NpgsqlDataSource.Create).ToArray();
    builder.Services.AddSingleton<SlaveDataSourcePool>(new SlaveDataSourcePool(slaveSources));
}
else
{
    builder.Services.AddSingleton<SlaveDataSourcePool>(new SlaveDataSourcePool([masterDataSource]));
}

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

await InitializeDatabase(masterDataSource, masterConn);

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
