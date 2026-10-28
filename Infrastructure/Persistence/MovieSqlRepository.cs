using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal class MovieSqlRepository(MovieManagerDbContext dbContext) : SqlRepository<Movie>(dbContext)
{
    protected override IQueryable<Movie> Query()
    {
        return DbContext.Movies
            .Include(movie => movie.Category)
            .Include(movie => movie.Actors)
            .AsSplitQuery();
    }

    protected override void ResolveReferences(Movie movie)
    {
        movie.Category = movie.Category?.Id > 0
            ? DbContext.Categories.Find(movie.Category.Id)
            : null;

        movie.Actors = movie.Actors
            .Select(actor => DbContext.Actors.Find(actor.Id))
            .OfType<Actor>()
            .DistinctBy(actor => actor.Id)
            .ToList();
    }

    protected override void CopyChanges(Movie tracked, Movie incoming)
    {
        base.CopyChanges(tracked, incoming);

        tracked.Category = incoming.Category?.Id > 0
            ? DbContext.Categories.Find(incoming.Category.Id)
            : null;

        tracked.Actors.Clear();
        foreach (int actorId in incoming.Actors.Select(actor => actor.Id).Where(id => id > 0).Distinct())
        {
            Actor? actor = DbContext.Actors.Find(actorId);
            if (actor is not null)
            {
                tracked.Actors.Add(actor);
            }
        }
    }
}
