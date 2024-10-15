// ReSharper disable InconsistentNaming

namespace Webinex.Calendar.Tests.Integration.Setups;

public static class Settings
{
    public static bool SKIP_DATABASE_CREATION
    {
        get
        {
            var environmentVariableValue =
                Environment.GetEnvironmentVariable("WEBINEX_CALENDAR_INTEGRATION_TESTS__SKIP_DATABASE_CREATION");
            return environmentVariableValue != null && bool.Parse(environmentVariableValue);
        }
    }
    
    public static string EVENTS_TABLE_NAME =>
        Environment.GetEnvironmentVariable("WEBINEX_CALENDAR_INTEGRATION_TESTS__EVENTS_TABLE_NAME") ?? "Events";

    public static string SCHEMA_NAME =>
        Environment.GetEnvironmentVariable("WEBINEX_CALENDAR_INTEGRATION_TESTS__SCHEMA_NAME") ?? "tests";

    public static string DB_NAME => Environment.GetEnvironmentVariable("WEBINEX_CALENDAR_INTEGRATION_TESTS__DB_NAME") ??
                                    "webinex_calendar_integration";

    public static string SQL_SERVER_CONNECTION_STRING =>
        Environment.GetEnvironmentVariable("WEBINEX_CALENDAR_INTEGRATION_TESTS__SQL_SERVER_CONNECTION_STRING") ??
        "Server=localhost;Trusted_Connection=True;TrustServerCertificate=True;";

    public static string SQL_DB_CONNECTION_STRING => $"{SQL_SERVER_CONNECTION_STRING}Database={DB_NAME};";
    
    /// <summary>
    /// Returns 1 of January 2023 in UTC, Sunday
    /// </summary>
    public static readonly DateTimeOffset JAN1_2023_UTC = new(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
}