using Microsoft.Data.Sqlite;

namespace writing_secure_code.Helpers;

public class LoginHelper
{
    private readonly string _connectionString;
    private const string AllowedSpecialCharacters = "!@#$%^&*?";

    public LoginHelper(string? connectionString = null)
    {
        _connectionString = connectionString ?? "Data Source=app.db";
    }

    public void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL,
                Email TEXT NOT NULL
            );";

        command.ExecuteNonQuery();
    }

    public bool RegisterUser(string username, string password)
    {
        if (!ValidationHelpers.IsValidInput(username) || !ValidationHelpers.IsValidInput(password, AllowedSpecialCharacters))
            return false;

        InitializeDatabase();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Users (Username, Password) VALUES ($username, $password)";
        command.Parameters.AddWithValue("$username", username);
        command.Parameters.AddWithValue("$password", password);

        try
        {
            command.ExecuteNonQuery();
            return true;
        }
        catch (SqliteException)
        {
            return false;
        }
    }

    public bool SaveUser(string username, string email)
    {
        if (!ValidationHelpers.IsValidInput(username) || !ValidationHelpers.IsValidInput(email, "@._+-"))
            return false;

        InitializeDatabase();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Users (Username, Email) VALUES ($username, $email)";
        command.Parameters.AddWithValue("$username", username);
        command.Parameters.AddWithValue("$email", email);

        try
        {
            command.ExecuteNonQuery();
            return true;
        }
        catch (SqliteException)
        {
            return false;
        }
    }

    public bool LoginUser(string username, string password)
    {
        if (!ValidationHelpers.IsValidInput(username) || !ValidationHelpers.IsValidInput(password, AllowedSpecialCharacters))
            return false;

        InitializeDatabase();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Password FROM Users WHERE Username = $username";
        command.Parameters.AddWithValue("$username", username);

        var storedPassword = command.ExecuteScalar()?.ToString();
        return storedPassword != null && storedPassword == password;
    }
}
