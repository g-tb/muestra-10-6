using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests;

[TestClass]
public class DependencyInjectionTest
{
    private ServiceProvider _provider = null!;

    [TestInitialize]
    public void Setup()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MovieManagerDatabase"] = "Server=localhost;Database=Unused;"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        _provider = services.BuildServiceProvider();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _provider.Dispose();
    }

    [TestMethod]
    public void AddInfrastructure_ResolvesRepositories_ToExpectedImplementations()
    {
        using IServiceScope scope = _provider.CreateScope();

        Assert.IsInstanceOfType<SqlRepository<Actor>>(scope.ServiceProvider.GetRequiredService<IRepository<Actor>>());
        Assert.IsInstanceOfType<SqlRepository<Category>>(scope.ServiceProvider.GetRequiredService<IRepository<Category>>());
        Assert.IsInstanceOfType<MovieSqlRepository>(scope.ServiceProvider.GetRequiredService<IRepository<Movie>>());
    }
}
