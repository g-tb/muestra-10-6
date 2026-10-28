using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests;

[TestClass]
public class SqlRepositoryTest
{
    private DbContextOptions<MovieManagerDbContext> _options = null!;

    [TestInitialize]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<MovieManagerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [TestMethod]
    public void ActorRepository_CrudOperations_PersistChanges()
    {
        Actor added;
        using (MovieManagerDbContext context = NewContext())
        {
            added = CreateActorRepository(context).Add(new Actor { Name = "Keanu", Bio = "Bio", BirthDate = new DateTime(1964, 9, 2) });
        }

        Assert.IsTrue(added.Id > 0);

        using (MovieManagerDbContext context = NewContext())
        {
            IRepository<Actor> actors = CreateActorRepository(context);
            Assert.AreEqual("Keanu", actors.Find(actor => actor.Name == "Keanu")!.Name);
            Assert.IsNull(actors.Find(actor => actor.Name == "Nobody"));
            Assert.AreEqual(1, actors.FindAll().Count);

            Actor? updated = actors.Update(new Actor { Id = added.Id, Name = "Keanu Reeves", Bio = "Updated", BirthDate = new DateTime(1964, 9, 3) });
            Assert.IsNotNull(updated);
            Assert.AreEqual("Keanu Reeves", updated.Name);
        }

        using (MovieManagerDbContext context = NewContext())
        {
            Actor stored = context.Actors.Single();
            Assert.AreEqual("Keanu Reeves", stored.Name);
            Assert.AreEqual("Updated", stored.Bio);
            Assert.AreEqual(new DateTime(1964, 9, 3), stored.BirthDate);
        }

        using (MovieManagerDbContext context = NewContext())
        {
            CreateActorRepository(context).Delete(added.Id);
        }

        using (MovieManagerDbContext context = NewContext())
        {
            Assert.AreEqual(0, context.Actors.Count());
        }
    }

    [TestMethod]
    public void CategoryRepository_CrudOperations_PersistChanges()
    {
        Category added;
        using (MovieManagerDbContext context = NewContext())
        {
            added = CreateCategoryRepository(context).Add(new Category { Name = "Drama" });
        }

        Assert.IsTrue(added.Id > 0);

        using (MovieManagerDbContext context = NewContext())
        {
            IRepository<Category> categories = CreateCategoryRepository(context);
            Assert.AreEqual(added.Id, categories.Find(category => category.Name == "Drama")!.Id);
            Assert.IsNull(categories.Find(category => category.Name == "Comedy"));
            Assert.AreEqual(1, categories.FindAll().Count);

            Category? updated = categories.Update(new Category { Id = added.Id, Name = "Thriller" });
            Assert.IsNotNull(updated);
            Assert.AreEqual("Thriller", updated.Name);
        }

        using (MovieManagerDbContext context = NewContext())
        {
            Assert.AreEqual("Thriller", context.Categories.Single().Name);
        }

        using (MovieManagerDbContext context = NewContext())
        {
            CreateCategoryRepository(context).Delete(added.Id);
        }

        using (MovieManagerDbContext context = NewContext())
        {
            Assert.AreEqual(0, context.Categories.Count());
        }
    }

    [TestMethod]
    public void Update_MissingId_ReturnsNull()
    {
        using MovieManagerDbContext context = NewContext();

        Assert.IsNull(CreateActorRepository(context).Update(new Actor { Id = 9999, Name = "Missing" }));
        Assert.IsNull(CreateCategoryRepository(context).Update(new Category { Id = 9999, Name = "Missing" }));
        Assert.IsNull(CreateMovieRepository(context).Update(new Movie { Id = 9999, Title = "Missing" }));
    }

    [TestMethod]
    public void Delete_MissingId_DoesNothing()
    {
        using (MovieManagerDbContext context = NewContext())
        {
            CreateActorRepository(context).Add(new Actor { Name = "Actor" });
            CreateCategoryRepository(context).Add(new Category { Name = "Category" });
            CreateMovieRepository(context).Add(new Movie { Title = "Movie" });
        }

        using (MovieManagerDbContext context = NewContext())
        {
            CreateActorRepository(context).Delete(9999);
            CreateCategoryRepository(context).Delete(9999);
            CreateMovieRepository(context).Delete(9999);
        }

        using (MovieManagerDbContext context = NewContext())
        {
            Assert.AreEqual(1, context.Actors.Count());
            Assert.AreEqual(1, context.Categories.Count());
            Assert.AreEqual(1, context.Movies.Count());
        }
    }

    [TestMethod]
    public void MovieAdd_WithDetachedCategoryAndActors_LinksExistingWithoutCopies()
    {
        (Category category, Actor actor) = SeedCategoryAndActor("Action", "Keanu");

        Movie added;
        using (MovieManagerDbContext context = NewContext())
        {
            added = CreateMovieRepository(context).Add(new Movie
            {
                Title = "The Matrix",
                Category = new Category { Id = category.Id, Name = "Detached copy" },
                Actors = [new Actor { Id = actor.Id }, new Actor { Id = actor.Id }, new Actor { Id = 9999 }]
            });
        }

        using (MovieManagerDbContext context = NewContext())
        {
            Assert.AreEqual(1, context.Categories.Count());
            Assert.AreEqual(1, context.Actors.Count());
            Assert.AreEqual("Action", context.Categories.Single().Name);

            Movie stored = context.Movies.Include(movie => movie.Category).Include(movie => movie.Actors)
                .Single(movie => movie.Id == added.Id);
            Assert.AreEqual(category.Id, stored.Category!.Id);
            Assert.AreEqual(1, stored.Actors.Count);
            Assert.AreEqual(actor.Id, stored.Actors[0].Id);
        }
    }

    [TestMethod]
    public void MovieFind_FreshContext_LoadsCategoryAndActors()
    {
        (Category category, Actor actor) = SeedCategoryAndActor("Action", "Keanu");
        using (MovieManagerDbContext context = NewContext())
        {
            CreateMovieRepository(context).Add(new Movie { Title = "The Matrix", Category = category, Actors = [actor] });
        }

        using (MovieManagerDbContext context = NewContext())
        {
            IRepository<Movie> movies = CreateMovieRepository(context);
            Movie found = movies.Find(movie => movie.Title == "The Matrix")!;

            Assert.AreEqual("Action", found.Category!.Name);
            Assert.IsTrue(found.HasActor(actor.Id));
            Assert.AreEqual("Keanu", found.Actors.Single().Name);

            Movie listed = movies.FindAll().Single();
            Assert.AreEqual("Action", listed.Category!.Name);
            Assert.AreEqual(1, listed.Actors.Count);
        }
    }

    [TestMethod]
    public void MovieUpdate_DetachedEntity_ReplacesCategoryAndActors()
    {
        (Category firstCategory, Actor firstActor) = SeedCategoryAndActor("Action", "Keanu");
        (Category secondCategory, Actor secondActor) = SeedCategoryAndActor("Drama", "Carrie-Anne");

        Movie added;
        using (MovieManagerDbContext context = NewContext())
        {
            added = CreateMovieRepository(context).Add(new Movie { Title = "The Matrix", Category = firstCategory, Actors = [firstActor] });
        }

        using (MovieManagerDbContext context = NewContext())
        {
            Movie? updated = CreateMovieRepository(context).Update(new Movie
            {
                Id = added.Id,
                Title = "The Matrix Reloaded",
                Director = "Wachowski",
                ReleaseYear = 2003,
                Category = new Category { Id = secondCategory.Id },
                Actors = [new Actor { Id = secondActor.Id }]
            });
            Assert.IsNotNull(updated);
        }

        using (MovieManagerDbContext context = NewContext())
        {
            Movie stored = LoadMovie(context, added.Id);
            Assert.AreEqual("The Matrix Reloaded", stored.Title);
            Assert.AreEqual("Wachowski", stored.Director);
            Assert.AreEqual(2003, stored.ReleaseYear);
            Assert.AreEqual(secondCategory.Id, stored.Category!.Id);
            Assert.AreEqual(1, stored.Actors.Count);
            Assert.AreEqual(secondActor.Id, stored.Actors[0].Id);
            Assert.AreEqual(2, context.Categories.Count());
            Assert.AreEqual(2, context.Actors.Count());
        }

        using (MovieManagerDbContext context = NewContext())
        {
            CreateMovieRepository(context).Update(new Movie { Id = added.Id, Title = "The Matrix Reloaded", Category = null, Actors = [] });
        }

        using (MovieManagerDbContext context = NewContext())
        {
            Movie stored = LoadMovie(context, added.Id);
            Assert.IsNull(stored.Category);
            Assert.AreEqual(0, stored.Actors.Count);
        }
    }

    [TestMethod]
    public void MovieUpdate_TrackedInstance_KeepsActors()
    {
        (Category category, Actor firstActor) = SeedCategoryAndActor("Action", "Keanu");
        Actor secondActor = SeedActor("Carrie-Anne");

        Movie added;
        using (MovieManagerDbContext context = NewContext())
        {
            added = CreateMovieRepository(context).Add(new Movie { Title = "The Matrix", Category = category, Actors = [firstActor, secondActor] });
        }

        using (MovieManagerDbContext context = NewContext())
        {
            IRepository<Movie> movies = CreateMovieRepository(context);
            Movie found = movies.Find(movie => movie.Id == added.Id)!;
            found.Title = "The Matrix Reloaded";

            Movie? updated = movies.Update(found);
            Assert.IsNotNull(updated);
        }

        using (MovieManagerDbContext context = NewContext())
        {
            Movie stored = LoadMovie(context, added.Id);
            Assert.AreEqual("The Matrix Reloaded", stored.Title);
            Assert.AreEqual(2, stored.Actors.Count);
            Assert.IsTrue(stored.HasActor(firstActor.Id));
            Assert.IsTrue(stored.HasActor(secondActor.Id));
        }
    }

    [TestMethod]
    public void MovieUpdate_TrackedInstanceWithAddedActor_KeepsAllActorsWithoutCopies()
    {
        (Category category, Actor firstActor) = SeedCategoryAndActor("Action", "Keanu");
        Actor secondActor = SeedActor("Carrie-Anne");
        Actor thirdActor = SeedActor("Laurence");

        Movie added;
        using (MovieManagerDbContext context = NewContext())
        {
            added = CreateMovieRepository(context).Add(new Movie { Title = "The Matrix", Category = category, Actors = [firstActor, secondActor] });
        }

        using (MovieManagerDbContext context = NewContext())
        {
            IRepository<Movie> movies = CreateMovieRepository(context);
            Movie found = movies.Find(movie => movie.Id == added.Id)!;
            found.Actors.Add(CreateActorRepository(context).Find(actor => actor.Id == thirdActor.Id)!);

            Movie? updated = movies.Update(found);
            Assert.IsNotNull(updated);
        }

        using (MovieManagerDbContext context = NewContext())
        {
            Movie stored = LoadMovie(context, added.Id);
            Assert.AreEqual(3, stored.Actors.Count);
            Assert.IsTrue(stored.HasActor(firstActor.Id));
            Assert.IsTrue(stored.HasActor(secondActor.Id));
            Assert.IsTrue(stored.HasActor(thirdActor.Id));
            Assert.AreEqual(3, context.Actors.Count());
        }
    }

    private Actor SeedActor(string actorName)
    {
        using MovieManagerDbContext context = NewContext();
        return CreateActorRepository(context).Add(new Actor { Name = actorName });
    }

    private (Category Category, Actor Actor) SeedCategoryAndActor(string categoryName, string actorName)
    {
        using MovieManagerDbContext context = NewContext();
        Category category = CreateCategoryRepository(context).Add(new Category { Name = categoryName });
        Actor actor = CreateActorRepository(context).Add(new Actor { Name = actorName });
        return (category, actor);
    }

    private static Movie LoadMovie(MovieManagerDbContext context, int id)
    {
        return context.Movies.Include(movie => movie.Category).Include(movie => movie.Actors)
            .Single(movie => movie.Id == id);
    }

    private MovieManagerDbContext NewContext()
    {
        return new MovieManagerDbContext(_options);
    }

    private static IRepository<Actor> CreateActorRepository(MovieManagerDbContext context)
    {
        return new SqlRepository<Actor>(context);
    }

    private static IRepository<Category> CreateCategoryRepository(MovieManagerDbContext context)
    {
        return new SqlRepository<Category>(context);
    }

    private static IRepository<Movie> CreateMovieRepository(MovieManagerDbContext context)
    {
        return new MovieSqlRepository(context);
    }
}
