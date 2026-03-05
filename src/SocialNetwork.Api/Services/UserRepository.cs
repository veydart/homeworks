using Npgsql;
using SocialNetwork.Api.Models;

namespace SocialNetwork.Api.Services;

public class UserRepository : IUserRepository
{
    private readonly NpgsqlDataSource _master;
    private readonly NpgsqlDataSource _slave;

    public UserRepository(
        [FromKeyedServices("master")] NpgsqlDataSource master,
        SlaveDataSourcePool slavePool)
    {
        _master = master;
        _slave = slavePool.GetNext();
    }

    public async Task<Guid> CreateAsync(RegisterRequest request, string passwordHash)
    {
        const string sql = """
            INSERT INTO users (first_name, last_name, birth_date, gender, interests, city, password_hash)
            VALUES (@firstName, @lastName, @birthDate, @gender, @interests, @city, @passwordHash)
            RETURNING id
            """;

        await using var cmd = _master.CreateCommand(sql);
        cmd.Parameters.AddWithValue("firstName", request.FirstName);
        cmd.Parameters.AddWithValue("lastName", request.LastName);
        cmd.Parameters.AddWithValue("birthDate", (object?)request.BirthDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("gender", (object?)request.Gender ?? DBNull.Value);
        cmd.Parameters.AddWithValue("interests", (object?)request.Interests ?? DBNull.Value);
        cmd.Parameters.AddWithValue("city", (object?)request.City ?? DBNull.Value);
        cmd.Parameters.AddWithValue("passwordHash", passwordHash);

        var result = await cmd.ExecuteScalarAsync();
        return (Guid)result!;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT id, first_name, last_name, birth_date, gender, interests, city
            FROM users
            WHERE id = @id
            """;

        await using var cmd = _slave.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new User
        {
            Id = reader.GetGuid(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            BirthDate = reader.IsDBNull(3) ? null : DateOnly.FromDateTime(reader.GetDateTime(3)),
            Gender = reader.IsDBNull(4) ? null : reader.GetString(4),
            Interests = reader.IsDBNull(5) ? null : reader.GetString(5),
            City = reader.IsDBNull(6) ? null : reader.GetString(6)
        };
    }

    public async Task<string?> GetPasswordHashByIdAsync(Guid id)
    {
        const string sql = "SELECT password_hash FROM users WHERE id = @id";

        await using var cmd = _slave.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", id);

        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }

    public async Task<List<User>> SearchAsync(string firstNamePrefix, string lastNamePrefix)
    {
        const string sql = """
            SELECT id, first_name, last_name, birth_date, gender, interests, city
            FROM users
            WHERE first_name LIKE @firstName AND last_name LIKE @lastName
            ORDER BY id
            """;

        await using var cmd = _slave.CreateCommand(sql);
        cmd.Parameters.AddWithValue("firstName", firstNamePrefix + "%");
        cmd.Parameters.AddWithValue("lastName", lastNamePrefix + "%");

        var users = new List<User>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new User
            {
                Id = reader.GetGuid(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                BirthDate = reader.IsDBNull(3) ? null : DateOnly.FromDateTime(reader.GetDateTime(3)),
                Gender = reader.IsDBNull(4) ? null : reader.GetString(4),
                Interests = reader.IsDBNull(5) ? null : reader.GetString(5),
                City = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }

        return users;
    }
}
