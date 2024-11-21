using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Api
{
    public static class LoginApi
    {
        /// <summary>
        /// Maps the authentication-related API endpoints.
        /// </summary>
        /// <param name="app">The WebApplication instance.</param>
        public static void MapLoginApi(this WebApplication app)
        {
            app.MapPost("/api/auth/login", LoginUser);
            app.MapPost("/api/auth/logout", LogoutUser);
            app.MapGet("/api/auth/currentuser", GetCurrentUser)
               .RequireAuthorization();
        }

        /// <summary>
        /// Handles user login by validating credentials and issuing a JWT token.
        /// </summary>
        /// <param name="loginData">The user credentials.</param>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>A result indicating success or failure.</returns>
        private static async Task<IResult> LoginUser(object loginData, HttpContext context)
        {
            try
            {
                // Deserialize login data
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var loginRequest = System.Text.Json.JsonSerializer.Deserialize<LoginRequest>(loginData.ToString(), options);

                if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Password))
                {
                    return Results.BadRequest("Invalid login request.");
                }

                // Establish connection to the SQLite database
                using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
                await connection.OpenAsync();

                // Prepare SQL command to retrieve user information
                var command = connection.CreateCommand();
                command.CommandText = "SELECT UserID, PasswordHash, Role, FirstName, LastName FROM Users WHERE Email = @Email";
                command.Parameters.AddWithValue("@Email", loginRequest.Email);

                using var reader = await command.ExecuteReaderAsync();

                // Check if user exists
                if (!await reader.ReadAsync())
                {
                    return Results.Unauthorized();
                }

                var storedHash = reader.GetString(1);

                // Verify password
                if (!VerifyPassword(loginRequest.Password, storedHash))
                {
                    return Results.Unauthorized();
                }

                var userId = reader.GetInt32(0);
                var role = reader.GetString(2);
                var firstName = reader.GetString(3);
                var lastName = reader.GetString(4);

                // Generate JWT token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(jwtKey);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Email, loginRequest.Email),
                        new Claim(ClaimTypes.Role, role)
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                // Return the token in the response body as JSON
                var loginResult = new
                {
                    Token = tokenString,
                    Expiration = tokenDescriptor.Expires
                };

                return Results.Ok(loginResult);
            }
            catch (Exception ex)
            {
                // In production, avoid returning detailed error messages
                return Results.Problem($"Error during login: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles user logout. In token-based authentication, this is typically handled on the client side.
        /// Optionally, implement token revocation if necessary.
        /// </summary>
        /// <returns>A result indicating success.</returns>
        private static IResult LogoutUser()
        {
            // Since JWTs are stateless, logout can be handled on the client side by removing the token.
            // Optionally, implement token revocation or blacklisting here.
            return Results.Ok();
        }

        /// <summary>
        /// Retrieves the current authenticated user's information.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>The current user's information.</returns>
        private static async Task<IResult> GetCurrentUser(HttpContext context)
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                var emailClaim = user.FindFirst(ClaimTypes.Email);
                var roleClaim = user.FindFirst(ClaimTypes.Role);

                if (userIdClaim == null || emailClaim == null || roleClaim == null)
                {
                    return Results.Unauthorized();
                }

                int userId = int.Parse(userIdClaim.Value);
                string email = emailClaim.Value;
                string role = roleClaim.Value;

                // Retrieve additional user details from the database
                using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT FirstName, LastName FROM Users WHERE UserID = @UserID";
                command.Parameters.AddWithValue("@UserID", userId);

                using var reader = await command.ExecuteReaderAsync();

                string firstName = string.Empty;
                string lastName = string.Empty;

                if (await reader.ReadAsync())
                {
                    firstName = reader.GetString(0);
                    lastName = reader.GetString(1);
                }

                var currentUser = new UserModel
                {
                    UserID = userId,
                    Email = email,
                    Role = role,
                    FirstName = firstName,
                    LastName = lastName
                };

                return Results.Ok(currentUser);
            }

            return Results.Unauthorized();
        }

        /// <summary>
        /// Represents the login request payload.
        /// </summary>
        private class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        /// <summary>
        /// Hashes a password using SHA256.
        /// Note: For enhanced security, consider using stronger hashing algorithms like BCrypt or Argon2.
        /// </summary>
        /// <param name="password">The plaintext password.</param>
        /// <returns>The hashed password.</returns>
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Verifies an entered password against the stored hashed password.
        /// </summary>
        /// <param name="enteredPassword">The entered plaintext password.</param>
        /// <param name="storedHash">The stored hashed password.</param>
        /// <returns>True if the password matches; otherwise, false.</returns>
        private static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var enteredHash = HashPassword(enteredPassword);
            return enteredHash == storedHash;
        }

        // Note: Replace this key management approach with a more secure method in production environments
        private static string jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                                     ?? "Your_Secret_Key_Should_Be_Long_Enough"; // Ensure this is stored securely
    }
}