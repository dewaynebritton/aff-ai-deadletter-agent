using Affinity.Deadletter.Agent.Models;
using Affinity.Deadletter.Agent.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.SqlClient;

namespace Affinity.Deadletter.Agent.Services;

public sealed class SqlDeadletterRepository : ISqlDeadletterRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlDeadletterRepository> _logger;

    public SqlDeadletterRepository(IConfiguration config, ILogger<SqlDeadletterRepository> logger)
    {
        _connectionString = config.GetConnectionString("DeadletterSql")
            ?? throw new InvalidOperationException("ConnectionStrings:DeadletterSql not configured.");
        _logger = logger;
    }

    public async Task<IReadOnlyList<DeadletterRecord>> GetUnprocessedDeadlettersAsync(CancellationToken ct = default)
    {
        var results = new List<DeadletterRecord>();

        const string sql = @"
            SELECT TOP (50)
                Id,
                CustomCorrelationId,
                InsertedDateUtc,
                RawPayloadJson
            FROM dbo.ServiceBusDeadletters
            WHERE ProcessedFlag = 0
            ORDER BY InsertedDateUtc;";

        //await using var conn = new SqlConnection(_connectionString);
        //await conn.OpenAsync(ct);

        //await using var cmd = new SqlCommand(sql, conn);
        //await using var reader = await cmd.ExecuteReaderAsync(ct);

        //while (await reader.ReadAsync(ct))
        //{
        //    results.Add(new DeadletterRecord
        //    {
        //        Id = reader.GetInt32(0),
        //        CustomCorrelationId = reader.IsDBNull(1) ? null : reader.GetString(1),
        //        InsertedDateUtc = reader.GetDateTime(2),
        //        RawPayloadJson = reader.IsDBNull(3) ? null : reader.GetString(3)
        //    });
        //}

        return results;
    }

    //public async Task MarkAsProcessedAsync(int id, CancellationToken ct = default)
    //{
    //    const string sql = @"
    //        UPDATE dbo.ServiceBusDeadletters
    //        SET ProcessedFlag = 1,
    //            ProcessedDateUtc = SYSUTCDATETIME()
    //        WHERE Id = @Id;";

    //    await using var conn = new SqlConnection(_connectionString);
    //    await conn.OpenAsync(ct);

    //    await using var cmd = new SqlCommand(sql, conn);
    //    cmd.Parameters.AddWithValue("@Id", id);

    //    await cmd.ExecuteNonQueryAsync(ct);
    //}
}
