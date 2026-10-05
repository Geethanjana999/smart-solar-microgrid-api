/*
 * File        : MongoDbOptions.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Strongly typed settings for the MongoDB connection.
 */
namespace SmartSolarMicrogrid.Api.Configuration;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDbOptions";

    // Local default only; real connection strings come from appsettings.json or environment variables.
    public string ConnectionString { get; set; } = "mongodb+srv://adminSolar:Password1234@cluster0.puq2f.mongodb.net/?appName=Cluster0";

    public string DatabaseName { get; set; } = "SmartMicrogrid";
}