using ProcurementApp.Data.Models;
using Npgsql;
using System.Data;
using Microsoft.Maui.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcurementApp.Data.Repositories;

public class CartRepository
{
    private readonly string _connectionString;

    public CartRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Получение элементов корзины с корректным полем cart_item_id
    public async Task<List<CartItem>> GetCartItemsAsync(int userId)
    {
        var cartItems = new List<CartItem>();
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        string query = @"
    SELECT ci.cart_item_id, ci.product_id, ci.quantity, 
           p.name, p.price, p.image_url
    FROM cart_items ci
    JOIN products p ON ci.product_id = p.product_id
    WHERE ci.user_id = @userId";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            cartItems.Add(new CartItem
            {
                CartItemId = reader.GetInt32(0),
                ProductId = reader.GetInt32(1),
                Quantity = reader.GetInt32(2),
                Product = new Product
                {
                    ProductId = reader.GetInt32(1),
                    Name = reader.GetString(3),
                    Price = reader.GetDecimal(4),
                    ImageUrl = reader.IsDBNull(5) ? null : reader.GetString(5)
                }
            });
        }
        return cartItems;
    }

    // <<< Новый метод: получает все товары в корзине с количеством в виде Dictionary
    public async Task<Dictionary<int, int>> GetCartQuantitiesAsync(int userId)
    {
        var quantities = new Dictionary<int, int>();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = "SELECT product_id, quantity FROM cart_items WHERE user_id = @userId";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            quantities.Add(reader.GetInt32(0), reader.GetInt32(1));
        }

        return quantities;
    }

    // Добавление в корзину с аутентификацией через SecureStorage
    public async Task<bool> AddToCartAsync(int productId, int quantity = 1)
    {
        var userIdString = await SecureStorage.Default.GetAsync("user_id");
        if (!int.TryParse(userIdString, out int userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }
        return await AddToCartAsync(userId, productId, quantity);
    }

    // Перегруженная версия добавления в корзину с проверкой пользователя
    public async Task<bool> AddToCartAsync(int userId, int productId, int quantity = 1)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            // Проверка существования пользователя
            using (var checkUser = new NpgsqlCommand(
                "SELECT COUNT(1) FROM users WHERE user_id = @userId", connection))
            {
                checkUser.Parameters.AddWithValue("@userId", userId);
                var userExists = (long)(await checkUser.ExecuteScalarAsync() ?? 0) > 0;
                if (!userExists) throw new KeyNotFoundException("User not found");
            }
            // Проверка существования товара
            using (var checkProductCommand = new NpgsqlCommand(
                "SELECT COUNT(1) FROM products WHERE product_id = @productId",
                connection))
            {
                checkProductCommand.Parameters.AddWithValue("@productId", productId);
                var productExists = (long)(await checkProductCommand.ExecuteScalarAsync() ?? 0) > 0;
                if (!productExists)
                {
                    throw new KeyNotFoundException("Товар не найден в базе данных");
                }
            }
            // Проверка наличия в корзине
            using var checkCartCommand = new NpgsqlCommand(
                "SELECT quantity FROM cart_items WHERE user_id = @userId AND product_id = @productId",
                connection);
            checkCartCommand.Parameters.AddWithValue("@userId", userId);
            checkCartCommand.Parameters.AddWithValue("@productId", productId);
            var existingQuantity = await checkCartCommand.ExecuteScalarAsync();
            if (existingQuantity != null)
            {
                // Используем атомарное увеличение количества
                using var updateCommand = new NpgsqlCommand(
                    "UPDATE cart_items SET quantity = quantity + @quantity " +
                    "WHERE user_id = @userId AND product_id = @productId",
                    connection);
                updateCommand.Parameters.AddWithValue("@quantity", quantity);
                updateCommand.Parameters.AddWithValue("@userId", userId);
                updateCommand.Parameters.AddWithValue("@productId", productId);
                return await updateCommand.ExecuteNonQueryAsync() > 0;
            }
            else
            {
                using var insertCommand = new NpgsqlCommand(
                    "INSERT INTO cart_items (user_id, product_id, quantity) " +
                    "VALUES (@userId, @productId, @quantity)",
                    connection);
                insertCommand.Parameters.AddWithValue("@userId", userId);
                insertCommand.Parameters.AddWithValue("@productId", productId);
                // Вставляем новую запись с quantity = 1
                insertCommand.Parameters.AddWithValue("@quantity", 1);
                return await insertCommand.ExecuteNonQueryAsync() > 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding to cart: {ex}");
            throw;
        }
    }

    // Обновление количества товара в корзине
    public async Task<bool> UpdateCartItemQuantityAsync(int cartItemId, int newQuantity)
    {
        if (newQuantity <= 0)
        {
            return await RemoveFromCartAsync(cartItemId);
        }
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(
            "UPDATE cart_items SET quantity = @quantity WHERE cart_item_id = @cartItemId",
            connection);
        command.Parameters.AddWithValue("@quantity", newQuantity);
        command.Parameters.AddWithValue("@cartItemId", cartItemId);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    // Получение текущего количества товара в корзине
    public async Task<int?> GetCartItemQuantityAsync(int userId, int productId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(
            "SELECT quantity FROM cart_items WHERE user_id = @userId AND product_id = @productId",
            connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@productId", productId);
        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : (int?)null;
    }

    // Удаление элемента корзины
    public async Task<bool> RemoveFromCartAsync(int cartItemId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(
            "DELETE FROM cart_items WHERE cart_item_id = @cartItemId",
            connection);
        command.Parameters.AddWithValue("@cartItemId", cartItemId);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    // Очистка корзины
    public async Task<bool> ClearCartAsync(int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(
            "DELETE FROM cart_items WHERE user_id = @userId",
            connection);
        command.Parameters.AddWithValue("@userId", userId);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateCartItemQuantityAsync(int userId, int productId, int newQuantity)
    {
        if (newQuantity <= 0)
        {
            return await RemoveFromCartByProductAsync(userId, productId);
        }
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        var cartItemId = await GetCartItemIdAsync(userId, productId);
        if (!cartItemId.HasValue)
        {
            return await AddToCartAsync(userId, productId, newQuantity);
        }
        using var command = new NpgsqlCommand(
            "UPDATE cart_items SET quantity = @quantity WHERE cart_item_id = @cartItemId",
            connection);
        command.Parameters.AddWithValue("@quantity", newQuantity);
        command.Parameters.AddWithValue("@cartItemId", cartItemId.Value);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private async Task<int?> GetCartItemIdAsync(int userId, int productId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(
            "SELECT cart_item_id FROM cart_items WHERE user_id = @userId AND product_id = @productId",
            connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@productId", productId);
        return (int?)await command.ExecuteScalarAsync();
    }

    public async Task<bool> RemoveFromCartByProductAsync(int userId, int productId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(
            "DELETE FROM cart_items WHERE user_id = @userId AND product_id = @productId",
            connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@productId", productId);
        return await command.ExecuteNonQueryAsync() > 0;
    }
}
