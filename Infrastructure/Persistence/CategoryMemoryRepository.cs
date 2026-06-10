using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence;

internal class CategoryMemoryRepository : ICategoryRepository
{
    private readonly List<Category> _categories = new();

    public Category Add(Category oneElement)
    {
        oneElement.Id = _categories.OrderByDescending(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefault() + 1;
        _categories.Add(oneElement);
        return oneElement;
    }

    public Category? Find(Func<Category, bool> filter)
    {
        return _categories.Where(filter).FirstOrDefault();
    }

    public IList<Category> FindAll()
    {
        return _categories;
    }

    public Category? Update(Category updatedEntity)
    {
        Category? foundCategory = Find(x => x.Id == updatedEntity.Id);
        if (foundCategory is null)
        {
            return null;
        }

        foundCategory = updatedEntity;
        return foundCategory;
    }

    public void Delete(int id)
    {
        _categories.RemoveAll(x => x.Id == id);
    }
}
