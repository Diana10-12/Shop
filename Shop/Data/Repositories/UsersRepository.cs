using ProcurementApp.Data.Models;
using Npgsql;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace ProcurementApp.Data.Repositories;

public class UsersRepository
{
    private readonly string _connectionString;

    public UsersRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<RegistrationResult> RegisterUserAsync(string email, string password, string firstName, string lastName, string userType)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Проверяем, существует ли уже пользователь с таким email
        using var checkCommand = new NpgsqlCommand(
            "SELECT COUNT(1) FROM users WHERE email = @email", connection);
        checkCommand.Parameters.AddWithValue("@email", email);

        var exists = (long)(await checkCommand.ExecuteScalarAsync() ?? 0) > 0;

        if (exists)
        {
            return new RegistrationResult { Success = false, ErrorMessage = "Пользователь с таким email уже существует" };
        }

        // Хешируем пароль
        var passwordHash = HashPassword(password);

        // Регистрируем пользователя
        using var command = new NpgsqlCommand(
            "INSERT INTO users (email, password_hash, first_name, last_name, user_type) " +
            "VALUES (@email, @passwordHash, @firstName, @lastName, @userType) RETURNING user_id", connection);

        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
        command.Parameters.AddWithValue("@firstName", firstName);
        command.Parameters.AddWithValue("@lastName", lastName);
        command.Parameters.AddWithValue("@userType", userType);

        try
        {
            var userId = await command.ExecuteScalarAsync();
            return new RegistrationResult { Success = true };
        }
        catch (Exception ex)
        {
            return new RegistrationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<LoginResult> LoginUserAsync(string email, string password)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Получаем данные пользователя
        using var command = new NpgsqlCommand(
            "SELECT user_id, password_hash, first_name, user_type FROM users WHERE email = @email", connection);
        command.Parameters.AddWithValue("@email", email);

        using var reader = await command.ExecuteReaderAsync();

        if (!reader.HasRows)
        {
            return new LoginResult { Success = false, ErrorMessage = "Пользователь с таким email не найден" };
        }

        await reader.ReadAsync();

        var userId = reader.GetInt32(0);
        var storedHash = reader.GetString(1);
        var firstName = reader.GetString(2);
        var userType = reader.GetString(3);

        // Проверяем пароль
        if (!VerifyPassword(password, storedHash))
        {
            return new LoginResult { Success = false, ErrorMessage = "Неверный пароль" };
        }

        // Генерируем токен (в реальном приложении используйте JWT или другой механизм)
        var token = GenerateToken(userId, email, userType);

        return new LoginResult
        {
            Success = true,
            Token = token,
            UserType = userType,
            FirstName = firstName,
            UserId = userId
        };
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == storedHash;
    }

    private string GenerateToken(int userId, string email, string userType)
    {
        // В реальном приложении используйте JWT с подписью и сроком действия
        var tokenData = $"{userId}:{email}:{userType}:{DateTime.UtcNow.Ticks}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenData));
    }
}

public class RegistrationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Token { get; set; }
    public string? UserType { get; set; }
    public string? FirstName { get; set; }
    public int UserId { get; set; }
}
