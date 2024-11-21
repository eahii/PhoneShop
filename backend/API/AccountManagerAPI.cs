using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Shared.Models;
using Shared.DTOs;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Api
{
    public static class AccountManagerApi
    {
        /// <summary>
        /// Maps the Account Manager API endpoints.
        /// </summary>
        public static void MapAccountManagerApi(this WebApplication app)
        {
            var group = app.MapGroup("/api/auth");
            group.MapPost("/register", RegisterUser);
            group.MapGet("/users", GetAllUsers).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
            group.MapDelete("/users/{id}", DeleteUser).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
            group.MapPut("/updateuser/{id}", UpdateUser).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        public static async Task<IResult> RegisterUser(RegisterRequest registerRequest)
        {
            try
            {
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync();

                    var checkUserCommand = connection.CreateCommand();
                    checkUserCommand.CommandText = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                    checkUserCommand.Parameters.AddWithValue("@Email", registerRequest.Email);

                    var exists = Convert.ToInt32(await checkUserCommand.ExecuteScalarAsync()) > 0;
                    if (exists)
                    {
                        return Results.BadRequest(new { Error = "Käyttäjä on jo olemassa." });
                    }

                    var passwordHash = HashPassword(registerRequest.Password);

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Users (Email, Role, PasswordHash, FirstName, LastName, Address, PhoneNumber, CreatedDate)
                        VALUES (@Email, @Role, @PasswordHash, @FirstName, @LastName, @Address, @PhoneNumber, CURRENT_TIMESTAMP)";
                    command.Parameters.AddWithValue("@Email", registerRequest.Email);
                    command.Parameters.AddWithValue("@Role", registerRequest.Role);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    command.Parameters.AddWithValue("@FirstName", registerRequest.FirstName ?? string.Empty);
                    command.Parameters.AddWithValue("@LastName", registerRequest.LastName ?? string.Empty);
                    command.Parameters.AddWithValue("@Address", registerRequest.Address ?? string.Empty);
                    command.Parameters.AddWithValue("@PhoneNumber", registerRequest.PhoneNumber ?? string.Empty);

                    await command.ExecuteNonQueryAsync();
                }

                return Results.Ok("Rekisteröinti onnistui.");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe käyttäjän rekisteröinnissä: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all users.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public static async Task<IResult> GetAllUsers()
        {
            try
            {
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT UserID, Email, Role, FirstName, LastName, Address, PhoneNumber, CreatedDate FROM Users";

                    var users = new List<UserModel>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new UserModel
                            {
                                UserID = reader.GetInt32(0),
                                Email = reader.GetString(1),
                                Role = reader.GetString(2),
                                FirstName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                LastName = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Address = reader.IsDBNull(5) ? null : reader.GetString(5),
                                PhoneNumber = reader.IsDBNull(6) ? null : reader.GetString(6),
                                CreatedDate = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7)
                            });
                        }
                    }

                    return Results.Ok(users);
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe käyttäjien hakemisessa: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing user.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public static async Task<IResult> UpdateUser(int id, UpdateUserRequest updateRequest)
        {
            try
            {
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync();

                    // Retrieve existing user
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT UserID, Email, Role, PasswordHash, FirstName, LastName, Address, PhoneNumber FROM Users WHERE UserID = @UserID";
                    command.Parameters.AddWithValue("@UserID", id);

                    using var reader = await command.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        return Results.NotFound($"Käyttäjää ID:llä {id} ei löytynyt.");
                    }

                    var user = new UserModel
                    {
                        UserID = reader.GetInt32(0),
                        Email = reader.GetString(1),
                        Role = reader.GetString(2),
                        PasswordHash = reader.GetString(3),
                        FirstName = reader.IsDBNull(4) ? null : reader.GetString(4),
                        LastName = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                        PhoneNumber = reader.IsDBNull(7) ? null : reader.GetString(7)
                    };

                    // Update fields if provided
                    user.Email = updateRequest.Email ?? user.Email;
                    user.Role = updateRequest.Role ?? user.Role;
                    user.PasswordHash = !string.IsNullOrEmpty(updateRequest.Password) ? HashPassword(updateRequest.Password) : user.PasswordHash;
                    user.FirstName = updateRequest.FirstName ?? user.FirstName;
                    user.LastName = updateRequest.LastName ?? user.LastName;
                    user.Address = updateRequest.Address ?? user.Address;
                    user.PhoneNumber = updateRequest.PhoneNumber ?? user.PhoneNumber;

                    // Update the user in the database
                    command = connection.CreateCommand();
                    command.CommandText = @"
                        UPDATE Users
                        SET Email = @Email,
                            Role = @Role,
                            PasswordHash = @PasswordHash,
                            FirstName = @FirstName,
                            LastName = @LastName,
                            Address = @Address,
                            PhoneNumber = @PhoneNumber
                        WHERE UserID = @UserID";
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@Role", user.Role);
                    command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    command.Parameters.AddWithValue("@FirstName", user.FirstName ?? string.Empty);
                    command.Parameters.AddWithValue("@LastName", user.LastName ?? string.Empty);
                    command.Parameters.AddWithValue("@Address", user.Address ?? string.Empty);
                    command.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber ?? string.Empty);
                    command.Parameters.AddWithValue("@UserID", id);

                    await command.ExecuteNonQueryAsync();
                    return Results.Ok(user);
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe käyttäjän tietojen päivittämisessä: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a user by ID.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public static async Task<IResult> DeleteUser(int id)
        {
            try
            {
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM Users WHERE UserID = @UserID";
                    command.Parameters.AddWithValue("@UserID", id);

                    var result = await command.ExecuteNonQueryAsync();
                    if (result == 0)
                    {
                        return Results.NotFound("Käyttäjää ei löytynyt.");
                    }

                    return Results.Ok("Käyttäjä poistettu onnistuneesti.");
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe käyttäjän poistamisessa: {ex.Message}");
            }
        }

        /// <summary>
        /// Hashes a password using SHA256.
        /// Note: For enhanced security, consider using stronger hashing algorithms like BCrypt or Argon2.
        /// </summary>
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}