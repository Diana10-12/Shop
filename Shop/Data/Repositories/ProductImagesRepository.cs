using ProcurementApp.Data.Models;
using Npgsql;
using System.Data;

namespace ProcurementApp.Data.Repositories;

public class ProductImagesRepository
{
    private readonly string _connectionString;

    public ProductImagesRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> AddImageAsync(int productId, string imageUrl, bool isPrimary = false)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            "INSERT INTO product_images (product_id, image_url, is_primary) " +
            "VALUES (@productId, @imageUrl, @isPrimary)", connection);

        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@imageUrl", imageUrl);
        command.Parameters.AddWithValue("@isPrimary", isPrimary);

        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<ProductImage>> GetImagesForProductAsync(int productId)
    {
        var images = new List<ProductImage>();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            "SELECT image_id, image_url, is_primary FROM product_images " +
            "WHERE product_id = @productId ORDER BY is_primary DESC", connection);

        command.Parameters.AddWithValue("@productId", productId);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            images.Add(new ProductImage
            {
                ImageId = reader.GetInt32(0),
                ImageUrl = reader.GetString(1),
                IsPrimary = reader.GetBoolean(2)
            });
        }

        return images;
    }
}
