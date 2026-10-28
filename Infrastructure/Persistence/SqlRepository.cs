using System.Linq.Expressions;
using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence;

internal class SqlRepository<T>(MovieManagerDbContext dbContext) : IRepository<T>
    where T : class, IEntity
{
    protected MovieManagerDbContext DbContext { get; } = dbContext;

    public T Add(T oneElement)
    {
        ResolveReferences(oneElement);
        DbContext.Set<T>().Add(oneElement);
        DbContext.SaveChanges();
        return oneElement;
    }

    public T? Find(Expression<Func<T, bool>> filter)
    {
        return Query().FirstOrDefault(filter);
    }

    public IList<T> FindAll()
    {
        return Query().ToList();
    }

    public T? Update(T updatedEntity)
    {
        T? trackedEntity = Query().FirstOrDefault(entity => entity.Id == updatedEntity.Id);
        if (trackedEntity is null)
        {
            return null;
        }

        CopyChanges(trackedEntity, updatedEntity);
        DbContext.SaveChanges();
        return trackedEntity;
    }

    public void Delete(int id)
    {
        T? foundEntity = Query().FirstOrDefault(entity => entity.Id == id);
        if (foundEntity is null)
        {
            return;
        }

        DbContext.Set<T>().Remove(foundEntity);
        DbContext.SaveChanges();
    }

    protected virtual IQueryable<T> Query()
    {
        return DbContext.Set<T>();
    }

    protected virtual void ResolveReferences(T entity)
    {
    }

    protected virtual void CopyChanges(T tracked, T incoming)
    {
        DbContext.Entry(tracked).CurrentValues.SetValues(incoming);
    }
}
