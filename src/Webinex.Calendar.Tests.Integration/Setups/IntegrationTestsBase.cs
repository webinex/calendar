﻿using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Webinex.Asky;
using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Tests.Integration.Setups;

public class IntegrationTestsBase
{
    private IServiceProvider _services = null!;

    protected IServiceScope Scope { get; private set; } = null!;
    protected IServiceProvider Services => Scope.ServiceProvider;
    protected SqlConnection SqlDbConnection { get; private set; } = null!;
    protected ICalendar<EventData> Calendar => Services.GetRequiredService<ICalendar<EventData>>();
    protected TestDbContext DbContext => Services.GetRequiredService<TestDbContext>();

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        services
            .AddScoped<TestDbContext>(_ => new TestDbContext())
            .AddScoped<ICalendarDbContext<EventData>>(x => x.GetRequiredService<TestDbContext>())
            .AddSingleton<IAskyFieldMap<EventData>>(new EventDataAskyFieldMap())
            .AddCalendar<EventData>();

        _services = services.BuildServiceProvider();

        Scope = _services.CreateScope();
    }

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        SqlDbConnection = new SqlConnection(SQL_DB_CONNECTION_STRING);
        SqlDbConnection.Open();

        if (!SKIP_DATABASE_CREATION)
            EnsureDatabaseExists();

        RecreateSchema();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Scope.Dispose();

        SqlDbConnection.Close();
        SqlDbConnection.Dispose();
    }

    protected void CleanDatabase()
    {
        ExecuteDbCommand($"DELETE FROM [{SCHEMA_NAME}].[{EVENTS_TABLE_NAME}]");
    }

    private void EnsureDatabaseExists()
    {
        var sql = $@"
IF NOT EXISTS (SELECT name FROM master.sys.databases WHERE name = N'{DB_NAME}')
    CREATE DATABASE {DB_NAME};
";

        using var sqlServerConnection = new SqlConnection(SQL_SERVER_CONNECTION_STRING);
        sqlServerConnection.Open();

        using var command = sqlServerConnection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private void RecreateSchema()
    {
        ExecuteDbCommand($@"
IF NOT EXISTS ( SELECT  *
                FROM    sys.schemas
                WHERE   name = N'{SCHEMA_NAME}' )
    EXEC('CREATE SCHEMA [{SCHEMA_NAME}]');");

        DropDatabaseTables();
        CreateDatabaseTables();
    }

    private void DropDatabaseTables()
    {
        ExecuteDbCommand($"DROP TABLE IF EXISTS [{SCHEMA_NAME}].[{EVENTS_TABLE_NAME}]");
    }

    private void CreateDatabaseTables()
    {
        var sql = $@"
create table {SCHEMA_NAME}.{EVENTS_TABLE_NAME}
(
    Id                                    uniqueidentifier not null
        constraint PK_Events primary key,
    Effective_Start                       datetimeoffset   not null,
    Effective_End                         datetimeoffset   null,
    Type                                  int              not null,
    RecurrentEventId                      uniqueidentifier null
        constraint FK_{EVENTS_TABLE_NAME}_RecurrentEventId_{EVENTS_TABLE_NAME}_Id foreign key references {SCHEMA_NAME}.{EVENTS_TABLE_NAME} (Id),
    Cancelled                             bit              not null,
    MoveTo_Start                          datetimeoffset   null,
    MoveTo_End                            datetimeoffset   null,

    Repeat_Type                           int              null,
    Repeat_Interval_StartSince1990Minutes bigint           null,
    Repeat_Interval_EndSince1990Minutes   bigint           null,
    Repeat_Interval_IntervalMinutes       int              null,
    Repeat_Interval_DurationMinutes       int              null,

    Repeat_Match_TimeOfTheDayUtcMinutes   int              null,
    Repeat_Match_DurationMinutes          int              null,
    Repeat_Match_OvernightDurationMinutes int              null,
    Repeat_Match_SameDayLastTime          int              null,
    Repeat_Match_Monday                   bit              null,
    Repeat_Match_Tuesday                  bit              null,
    Repeat_Match_Wednesday                bit              null,
    Repeat_Match_Thursday                 bit              null,
    Repeat_Match_Friday                   bit              null,
    Repeat_Match_Saturday                 bit              null,
    Repeat_Match_Sunday                   bit              null,
    Repeat_Match_DayOfMonth               int              null,
    Data_Name                             nvarchar(200)    not null,
)
";
        
        ExecuteDbCommand(sql);
    }

    private void ExecuteDbCommand(string sql)
    {
        using var command = SqlDbConnection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}