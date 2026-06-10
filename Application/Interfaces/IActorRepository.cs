using Domain.Entities;

namespace Application.Interfaces;

public interface IActorRepository
{
    Actor Add(Actor oneElement);
    Actor? Find(Func<Actor, bool> filter);
    IList<Actor> FindAll();
    Actor? Update(Actor updatedEntity);
    void Delete(int id);
}
