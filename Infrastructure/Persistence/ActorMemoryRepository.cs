using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence;

internal class ActorMemoryRepository : IActorRepository
{
    private readonly List<Actor> _actors = new();

    public Actor Add(Actor oneElement)
    {
        oneElement.Id = _actors.OrderByDescending(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefault() + 1;
        _actors.Add(oneElement);
        return oneElement;
    }

    public Actor? Find(Func<Actor, bool> filter)
    {
        return _actors.Where(filter).FirstOrDefault();
    }

    public IList<Actor> FindAll()
    {
        return _actors;
    }

    public Actor? Update(Actor updatedEntity)
    {
        Actor? foundActor = Find(x => x.Id == updatedEntity.Id);
        if (foundActor is null)
        {
            return null;
        }

        foundActor = updatedEntity;
        return foundActor;
    }

    public void Delete(int id)
    {
        _actors.RemoveAll(x => x.Id == id);
    }
}
