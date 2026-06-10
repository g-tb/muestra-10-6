using Domain.Entities;

namespace Application.Interfaces;

public interface IMovieRepository
{
    Movie Add(Movie oneElement);
    Movie? Find(Func<Movie, bool> filter);
    IList<Movie> FindAll();
    Movie? Update(Movie updatedEntity);
    void Delete(int id);
}
