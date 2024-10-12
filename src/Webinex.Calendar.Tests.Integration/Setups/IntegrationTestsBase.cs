using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

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
            .AddCalendar<EventData>(x => x
                .AddDbContext<TestDbContext>()
                .AddAskyFieldMap<EventDataAskyFieldMap>());

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
    Effective_Start                       bigint           not null,
    Effective_End                         bigint           null,
    Type                                  int              not null,
    RecurrentEventId                      uniqueidentifier null
        constraint FK_{EVENTS_TABLE_NAME}_RecurrentEventId_{EVENTS_TABLE_NAME}_Id foreign key references {SCHEMA_NAME}.{EVENTS_TABLE_NAME} (Id),
    Cancelled                             bit              not null,
    MoveTo_Start                          datetimeoffset   null,
    MoveTo_End                            datetimeoffset   null,

    Repeat_Type                           int              null,
    Repeat_Interval                       int              null,
    Repeat_DurationMinutes                int              null,
    Repeat_TimeOfTheDayInMinutes          int              null,
    Repeat_OvernightDurationMinutes       int              null,
    Repeat_SameDayLastTime                int              null,
    Repeat_TimeZone                       nvarchar(50)     null,
    Repeat_Monday                         bit              null,
    Repeat_Tuesday                        bit              null,
    Repeat_Wednesday                      bit              null,
    Repeat_Thursday                       bit              null,
    Repeat_Friday                         bit              null,
    Repeat_Saturday                       bit              null,
    Repeat_Sunday                         bit              null,
    Repeat_DayOfMonth                     int              null,
    Data_Name                             nvarchar(200)    not null,
    Data_NValue                           nvarchar(250)    null,
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