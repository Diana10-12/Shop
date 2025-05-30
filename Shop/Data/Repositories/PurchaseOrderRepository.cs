using ProcurementApp.Data.Models;
using Npgsql;
using System.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcurementApp.Data.Repositories
{
    public class PurchaseOrderRepository
    {
        private readonly string _connectionString;

        public PurchaseOrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> CreateOrderAsync(int userId, decimal totalAmount, DateTime estimatedDeliveryDate)
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

            // Создание заказа с добавлением estimated_delivery_date
            using var command = new NpgsqlCommand(
                "INSERT INTO purchase_orders (user_id, total_amount, estimated_delivery_date) " +
                "VALUES (@userId, @totalAmount, @estimatedDeliveryDate) RETURNING order_id",
                connection);

            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@totalAmount", totalAmount);
            command.Parameters.AddWithValue("@estimatedDeliveryDate", estimatedDeliveryDate);

            return (int)(await command.ExecuteScalarAsync() ?? 0);
        }

        public async Task<PurchaseOrder> GetOrderByIdAsync(int orderId)
        {
            Console.WriteLine($"Fetching order with ID: {orderId}");

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT 
                    order_id, 
                    user_id, 
                    status, 
                    total_amount, 
                    created_at, 
                    updated_at, 
                    estimated_delivery_date 
                FROM purchase_orders 
                WHERE order_id = @orderId";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@orderId", orderId);

            try
            {
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var order = new PurchaseOrder
                    {
                        OrderId = reader.GetInt32(0),        // order_id
                        UserId = reader.GetInt32(1),         // user_id
                        Status = reader.GetString(2),        // status
                        TotalAmount = reader.GetDecimal(3),  // total_amount
                        CreatedAt = reader.GetDateTime(4),   // created_at
                        UpdatedAt = reader.GetDateTime(5),   // updated_at
                        EstimatedDeliveryDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6) // estimated_delivery_date
                    };

                    Console.WriteLine($"Order found: {order.OrderId}, created at: {order.CreatedAt}");
                    return order;
                }

                Console.WriteLine($"No order found with ID: {orderId}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching order: {ex.Message}");
                return null;
            }
        }

        public async Task<List<PurchaseOrder>> GetOrdersByUserIdAsync(int userId)
        {
            Console.WriteLine($"Fetching orders for user ID: {userId}");

            var orders = new List<PurchaseOrder>();
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT 
                    order_id, 
                    user_id, 
                    status, 
                    total_amount, 
                    created_at, 
                    updated_at, 
                    estimated_delivery_date 
                FROM purchase_orders 
                WHERE user_id = @userId
                ORDER BY created_at DESC";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);

            try
            {
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    orders.Add(new PurchaseOrder
                    {
                        OrderId = reader.GetInt32(0),
                        UserId = reader.GetInt32(1),
                        Status = reader.GetString(2),
                        TotalAmount = reader.GetDecimal(3),
                        CreatedAt = reader.GetDateTime(4),
                        UpdatedAt = reader.GetDateTime(5),
                        EstimatedDeliveryDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
                    });
                }

                Console.WriteLine($"Found {orders.Count} orders for user {userId}");
                return orders;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching orders for user {userId}: {ex.Message}");
                return new List<PurchaseOrder>();
            }
        }
    }
}
