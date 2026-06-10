using Domain.Entities;

namespace Application.Interfaces;

public interface ICategoryRepository
{
    Category Add(Category oneElement);
    Category? Find(Func<Category, bool> filter);
    IList<Category> FindAll();
    Category? Update(Category updatedEntity);
    void Delete(int id);
}
