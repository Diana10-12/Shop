using ProcurementApp.Data.Models;
using Npgsql;
using System.Data;

namespace ProcurementApp.Data.Repositories;

public class DeliveryRepository
{
    private readonly string _connectionString;

    public DeliveryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<int> CreateDeliveryAsync(Delivery delivery)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            "INSERT INTO deliveries (order_id, address, delivery_cost, estimated_days, weather_impact, status) " +
            "VALUES (@orderId, @address, @deliveryCost, @estimatedDays, @weatherImpact, @status) " +
            "RETURNING delivery_id", connection);

        command.Parameters.AddWithValue("@orderId", delivery.OrderId);
        command.Parameters.AddWithValue("@address", delivery.Address);
        command.Parameters.AddWithValue("@deliveryCost", delivery.DeliveryCost);
        command.Parameters.AddWithValue("@estimatedDays", delivery.EstimatedDays);
        command.Parameters.AddWithValue("@weatherImpact", (object)delivery.WeatherImpact ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", delivery.Status);

        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<Delivery> GetDeliveryByOrderIdAsync(int orderId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            "SELECT delivery_id, address, delivery_cost, estimated_days, weather_impact, status " +
            "FROM deliveries WHERE order_id = @orderId", connection);

        command.Parameters.AddWithValue("@orderId", orderId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Delivery
            {
                DeliveryId = reader.GetInt32(0),
                Address = reader.GetString(1),
                DeliveryCost = reader.GetDecimal(2),
                EstimatedDays = reader.GetInt32(3),
                WeatherImpact = reader.IsDBNull(4) ? null : reader.GetString(4),
                Status = reader.GetString(5)
            };
        }

        return null;
    }
}
