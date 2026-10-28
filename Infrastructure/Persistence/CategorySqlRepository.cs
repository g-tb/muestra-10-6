using Application.Interfaces;
using System.Linq.Expressions;
using Domain.Entities;

namespace Infrastructure.Persistence;

internal class CategorySqlRepository(MovieManagerDbContext dbContext) : IRepository<Category>
{
    public Category Add(Category oneElement)
    {
        dbContext.Categories.Add(oneElement);
        dbContext.SaveChanges();
        return oneElement;
    }

    public Category? Find(Expression<Func<Category, bool>> filter)
    {
        return dbContext.Categories.FirstOrDefault(filter);
    }

    public IList<Category> FindAll()
    {
        return dbContext.Categories.ToList();
    }

    public Category? Update(Category updatedEntity)
    {
        Category? foundCategory = dbContext.Categories.Find(updatedEntity.Id);
        if (foundCategory is null)
        {
            return null;
        }

        foundCategory.Name = updatedEntity.Name;
        dbContext.SaveChanges();
        return foundCategory;
    }

    public void Delete(int id)
    {
        Category? foundCategory = dbContext.Categories.Find(id);
        if (foundCategory is null)
        {
            return;
        }

        dbContext.Categories.Remove(foundCategory);
        dbContext.SaveChanges();
    }
}
