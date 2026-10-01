/*
 * File        : Roles.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Role name constants shared by token issuing and [Authorize] attributes.
 */
namespace SmartSolarMicrogrid.Api.Configuration;

public static class Roles
{
    public const string Backoffice = "Backoffice";
    public const string GridOperator = "GridOperator";
    public const string Prosumer = "Prosumer";

    public const string BackofficeOrOperator = Backoffice + "," + GridOperator;
    public const string All = Backoffice + "," + GridOperator + "," + Prosumer;

    // Roles that can be created as web users.
    public static readonly string[] Staff = { Backoffice, GridOperator };

    // Every role the system recognises; used to validate role input.
    public static readonly string[] Valid = { Backoffice, GridOperator, Prosumer };
}
