using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IMovieService, MovieService>();
        services.AddSingleton<IActorService, ActorService>();
        services.AddSingleton<ICategoryService, CategoryService>();

        return services;
    }
}
