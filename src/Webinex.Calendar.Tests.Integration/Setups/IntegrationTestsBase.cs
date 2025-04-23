using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Webinex.Asky;
using Webinex.Calendar.EntityFramework;

namespace Webinex.Calendar.Tests.Integration.Setups;

public class IntegrationTestsBase
{
    private static bool _initialized = false;
    private static readonly object Lock = new object();
    
    private IServiceProvider _services = null!;

    protected IServiceScope Scope { get; private set; } = null!;
    protected IServiceProvider Services => Scope.ServiceProvider;
    protected ICalendar<EventData> Calendar => Services.GetRequiredService<ICalendar<EventData>>();
    protected TestDbContext DbContext => Services.GetRequiredService<TestDbContext>();

    [OneTimeSetUp]
    public void IntegrationTestsBase_OneTimeSetup()
    {
        var services = new ServiceCollection();

        services
            .AddScoped<TestDbContext>(_ => new TestDbContext())
            .AddCalendar<EventData>(x => x
                .UseDbContext<TestDbContext>());

        services.AddSingleton<IAskyFieldMap<EventData>, EventDataAskyFieldMap>();

        _services = services.BuildServiceProvider();

        Scope = _services.CreateScope();

        Initialize();
    }

    [OneTimeTearDown]
    public void IntegrationTestsBase_OneTimeTearDown()
    {
        Scope.Dispose();
    }

    protected void CleanDatabase()
    {
        DbContext.Set<EventRow<EventData>>().ExecuteDelete();
        DbContext.Set<RecurrentEventRow<EventData>>().ExecuteDelete();
    }

    private void Initialize()
    {
        if (SKIP_DATABASE_CREATION)
            return;
        
        if (_initialized)
            return;

        lock (Lock)
        {
            if (_initialized)
                return;

            RecreateDatabase();
            _initialized = true;
        }
    }

    private void RecreateDatabase()
    {
        using var scope = Scope.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }
}