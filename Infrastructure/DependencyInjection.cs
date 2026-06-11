using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IRepository<Movie>, MovieMemoryRepository>();
        services.AddSingleton<IRepository<Actor>, ActorMemoryRepository>();
        services.AddSingleton<IRepository<Category>, CategoryMemoryRepository>();

        return services;
    }
}
