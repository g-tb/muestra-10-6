using Application.Interfaces;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IMovieRepository, MovieMemoryRepository>();
        services.AddSingleton<IActorRepository, ActorMemoryRepository>();
        services.AddSingleton<ICategoryRepository, CategoryMemoryRepository>();

        return services;
    }
}
