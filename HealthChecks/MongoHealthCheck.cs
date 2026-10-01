/*
 * File        : MongoHealthCheck.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Health check that pings MongoDB; used by IIS/monitoring at /health.
 */
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Data;

namespace SmartSolarMicrogrid.Api.HealthChecks;

public sealed class MongoHealthCheck(MongoContext db) : IHealthCheck
{
    // Runs the MongoDB "ping" command; unhealthy when the database cannot be reached.
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB unreachable.", ex);
        }
    }
}
