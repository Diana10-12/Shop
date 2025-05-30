using ProcurementApp.Data.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcurementApp.Data.Repositories;

public class ProductsRepository
{
    private readonly string _connectionString;
    private readonly ProductImagesRepository _imagesRepository;

    public ProductsRepository(string connectionString, ProductImagesRepository imagesRepository)
    {
        _connectionString = connectionString;
        _imagesRepository = imagesRepository;
    }

    public ProductsRepository(string connectionString)
    {
        _connectionString = connectionString;
        _imagesRepository = null;
    }

    private async Task<List<Product>> FetchProductsFromDbAsync(string query, Action<NpgsqlCommand> addParametersAction = null)
    {
        var products = new List<Product>();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(query, connection);

        addParametersAction?.Invoke(command);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new Product
            {
                ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
                StockQuantity = reader.IsDBNull(reader.GetOrdinal("quantity")) ? 0 : reader.GetInt32(reader.GetOrdinal("quantity")),
            });
        }
        return products;
    }

    private void EnsureImagesRepository()
    {
        if (_imagesRepository == null)
        {
            throw new InvalidOperationException(
               "ProductImagesRepository не был внедрен через конструктор. Загрузка изображений невозможна.");
        }
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        EnsureImagesRepository();

        string query = "SELECT product_id, name, description, price, image_url, quantity FROM products";
        var products = await FetchProductsFromDbAsync(query);

        foreach (var product in products)
        {
            product.Images = await _imagesRepository.GetImagesForProductAsync(product.ProductId);
        }
        return products;
    }

    public async Task<int> AddProductAsync(Product product)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            "INSERT INTO products (name, description, price, image_url, seller_id, quantity) " +
            "VALUES (@name, @description, @price, @imageUrl, @sellerId, @quantity) RETURNING product_id",
            connection);

        command.Parameters.AddWithValue("@name", product.Name);
        command.Parameters.AddWithValue("@description", (object)product.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@price", product.Price);
        command.Parameters.AddWithValue("@imageUrl", (object)product.ImageUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("@quantity", product.StockQuantity);
        command.Parameters.AddWithValue("@sellerId", product.SellerId);

        try
        {
            var productId = await command.ExecuteScalarAsync();
            return Convert.ToInt32(productId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при добавлении товара: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Product>> GetSellerProductsAsync(int sellerId)
    {
        EnsureImagesRepository();

        var products = new List<Product>();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = @"
            SELECT product_id, name, description, price, image_url, quantity  
            FROM products 
            WHERE seller_id = @sellerId";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@sellerId", sellerId);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var product = new Product
            {
                ProductId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Price = reader.GetDecimal(3),
                ImageUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                StockQuantity = reader.GetInt32(5)
            };

            product.Images = await _imagesRepository.GetImagesForProductAsync(product.ProductId);

            products.Add(product);
        }

        return products;
    }

    public async Task<bool> UpdateProductQuantityAsync(int productId, int delta)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(
            "UPDATE products SET quantity = GREATEST(quantity + @delta, 0) WHERE product_id = @productId",
            connection);
        command.Parameters.AddWithValue("@delta", delta);
        command.Parameters.AddWithValue("@productId", productId);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteProductAsync(int productId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            "DELETE FROM products WHERE product_id = @productId",
            connection);

        command.Parameters.AddWithValue("@productId", productId);

        int rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<Product> GetProductByIdAsync(int productId)
    {
        Product product = null;
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = @"
            SELECT product_id, name, description, price, image_url, seller_id, quantity  
            FROM products 
            WHERE product_id = @productId";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@productId", productId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            product = new Product
            {
                ProductId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Price = reader.GetDecimal(3),
                ImageUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                SellerId = reader.GetInt32(5),
                StockQuantity = reader.GetInt32(6)
            };
        }

        if (product != null && _imagesRepository != null)
        {
            product.Images = await _imagesRepository.GetImagesForProductAsync(product.ProductId);
        }

        return product;
    }
}
