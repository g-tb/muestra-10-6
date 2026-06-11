using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence;

internal class MovieMemoryRepository : IRepository<Movie>
{
    private readonly List<Movie> _movies = new();

    public Movie Add(Movie oneElement)
    {
        oneElement.Id = _movies.OrderByDescending(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefault() + 1;
        _movies.Add(oneElement);
        return oneElement;
    }

    public Movie? Find(Func<Movie, bool> filter)
    {
        return _movies.Where(filter).FirstOrDefault();
    }

    public IList<Movie> FindAll()
    {
        return _movies;
    }

    public Movie? Update(Movie updatedEntity)
    {
        Movie? foundMovie = Find(x => x.Id == updatedEntity.Id);
        foundMovie?.Update(updatedEntity);
        return foundMovie;
    }

    public void Delete(int id)
    {
        _movies.RemoveAll(x => x.Id == id);
    }
}
