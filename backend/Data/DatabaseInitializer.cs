using Microsoft.Data.Sqlite;
using Shared.Models;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Data
{
    public static class DatabaseInitializer
    {
        public static async Task Initialize()
        {
            using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
            {
                await connection.OpenAsync();

                // Create Phones table
                var createPhonesTable = connection.CreateCommand();
                createPhonesTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Phones (
                        PhoneID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Brand TEXT NOT NULL,
                        Model TEXT NOT NULL,
                        Price REAL NOT NULL,
                        Description TEXT,
                        Condition TEXT NOT NULL,
                        StockQuantity INTEGER
                    )";
                await createPhonesTable.ExecuteNonQueryAsync();

                // Create Users table
                var createUsersTable = connection.CreateCommand();
                createUsersTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Role TEXT NOT NULL,
                        Email TEXT NOT NULL UNIQUE,
                        PasswordHash TEXT NOT NULL,
                        FirstName TEXT NOT NULL,
                        LastName TEXT NOT NULL,
                        Address TEXT,
                        PhoneNumber TEXT,
                        CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                    )";
                await createUsersTable.ExecuteNonQueryAsync();

                // Create Orders table
                var createOrdersTable = connection.CreateCommand();
                createOrdersTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Orders (
                        OrderID INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserID INTEGER NOT NULL,
                        OrderDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        TotalPrice REAL NOT NULL,
                        Status TEXT,
                        FOREIGN KEY (UserID) REFERENCES Users(UserID)
                    )";
                await createOrdersTable.ExecuteNonQueryAsync();

                // Create Offers table
                var createOffersTable = connection.CreateCommand();
                createOffersTable.CommandText = @"
                CREATE TABLE IF NOT EXISTS Offers (
                    OfferID INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserID INTEGER NOT NULL,
                    PhoneBrand TEXT NOT NULL,
                    PhoneModel TEXT NOT NULL,
                    OriginalPrice REAL NOT NULL,
                    PhoneAge INTEGER,
                    OverallCondition INTEGER,
                    BatteryLife INTEGER,
                    ScreenCondition INTEGER,
                    CameraCondition INTEGER,
                    Status TEXT,
                    SubmissionDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (UserID) REFERENCES Users(UserID)
                )";
                await createOffersTable.ExecuteNonQueryAsync();

                // Create ShoppingCart table
                var createShoppingCartTable = connection.CreateCommand();
                createShoppingCartTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ShoppingCart (
                        CartID INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserID INTEGER NOT NULL UNIQUE,
                        FOREIGN KEY (UserID) REFERENCES Users(UserID)
                    )";
                await createShoppingCartTable.ExecuteNonQueryAsync();

                // Create CartItems table
                var createCartItemsTable = connection.CreateCommand();
                createCartItemsTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS CartItems (
                        CartItemID INTEGER PRIMARY KEY AUTOINCREMENT,
                        CartID INTEGER NOT NULL,
                        PhoneID INTEGER NOT NULL,
                        Quantity INTEGER NOT NULL,
                        FOREIGN KEY (CartID) REFERENCES ShoppingCart(CartID),
                        FOREIGN KEY (PhoneID) REFERENCES Phones(PhoneID)
                    )";
                await createCartItemsTable.ExecuteNonQueryAsync();

                // Seed Admin user
                await SeedData(connection);
                await SeedPhones(connection);
            }
        }

        private static async Task SeedData(SqliteConnection connection)
        {
            var checkAdminCommand = connection.CreateCommand();
            checkAdminCommand.CommandText = "SELECT COUNT(*) FROM Users WHERE Email = 'admin@usedphoneshop.com'";
            var adminExists = (long)await checkAdminCommand.ExecuteScalarAsync() > 0;

            if (!adminExists)
            {
                var insertAdminCommand = connection.CreateCommand();

                // Create password hash
                string adminPassword = "Admin123!"; // Ensure this is secure and consider hashing this securely
                string passwordHash = HashPassword(adminPassword);

                insertAdminCommand.CommandText = @"
                    INSERT INTO Users (Role, Email, PasswordHash, FirstName, LastName, Address, PhoneNumber)
                    VALUES ('Admin', 'admin@usedphoneshop.com', @PasswordHash, 'Admin', 'User', '123 Admin Street', '123456789')";
                insertAdminCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);

                await insertAdminCommand.ExecuteNonQueryAsync();
                Console.WriteLine("Admin käyttäjä lisätty: admin@usedphoneshop.com");
            }
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private static async Task SeedPhones(SqliteConnection connection)
        {
            var checkPhonesCommand = connection.CreateCommand();
            checkPhonesCommand.CommandText = "SELECT COUNT(*) FROM Phones";
            var phonesExist = (long)await checkPhonesCommand.ExecuteScalarAsync() > 0;

            if (!phonesExist)
            {
                var insertPhonesCommand = connection.CreateCommand();
                insertPhonesCommand.CommandText = @"
                    INSERT INTO Phones (Brand, Model, Price, Description, Condition, StockQuantity) VALUES
                    ('Apple', 'iPhone 12', 799.99, 'Latest model with A14 Bionic chip', 'New', 10),
                    ('Samsung', 'Galaxy S21', 699.99, 'Flagship model with Exynos 2100', 'New', 15),
                    ('Google', 'Pixel 5', 599.99, '5G capable with Snapdragon 765G', 'New', 8),
                    ('OnePlus', '8T', 499.99, 'Fast and smooth with Snapdragon 865', 'New', 12)";
                await insertPhonesCommand.ExecuteNonQueryAsync();
                Console.WriteLine("Esimerkkipuhelimet lisätty.");
            }
        }
    }
}