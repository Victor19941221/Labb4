using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using Labb4.Models;
using Labb4.Components.Pages;

public class ActorService
{
    private readonly IMongoCollection<Actor> _actors;
    private readonly IMongoCollection<Movie> _movies;
    private readonly MovieService _movieService; 

    public ActorService(IOptions<DatabaseSettings> databaseSettings, MovieService movieService)
    {
        var mongoClient = new MongoClient(databaseSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseSettings.Value.DatabaseName);

        _movies = mongoDatabase.GetCollection<Movie>(databaseSettings.Value.MoviesCollectionName);
        _actors = mongoDatabase.GetCollection<Actor>(databaseSettings.Value.ActorsCollectionName);
        _movieService = movieService; 
    }

    // Get all actors
    public async Task<List<Actor>> GetActorsAsync() =>
        await _actors.Find(actor => true).ToListAsync();

    // Get a specific actor by Id
    public async Task<Actor> GetActorByIdAsync(string id) =>
        await _actors.Find(actor => actor.Id == id).FirstOrDefaultAsync();

    // Create a new actor
    public async Task CreateActorAsync(Actor actor) =>
        await _actors.InsertOneAsync(actor);

    public async Task AddMovieToActorAsync(string actorId, ObjectId movieId)
    {
        if (string.IsNullOrEmpty(actorId)) return;

        var filter = Builders<Actor>.Filter.Eq(a => a.Id, actorId);
        var update = Builders<Actor>.Update.AddToSet(a => a.MovieIds, movieId);
        await _actors.UpdateOneAsync(filter, update);
    }


    // Update an existing actor
    public async Task UpdateActorAsync(string actorId, Actor updatedActor)
    {
        // Step 1: Fetch the current actor details to compare previous MovieIds
        var actorToUpdate = await _actors.Find(a => a.Id == actorId).FirstOrDefaultAsync();
        if (actorToUpdate == null)
        {
            Console.WriteLine("Actor not found.");
            return;
        }

        // Step 2: Determine movies to add and remove based on the updated MovieIds
        var previousMovieIds = actorToUpdate.MovieIds ?? new List<ObjectId>();
        var currentMovieIds = updatedActor.MovieIds ?? new List<ObjectId>();

        // Movies to add the actor to
        var moviesToAddActor = currentMovieIds.Except(previousMovieIds);

        // Movies to remove the actor from
        var moviesToRemoveActor = previousMovieIds.Except(currentMovieIds);

        // Step 3: Update the actor document with new properties
        var filter = Builders<Actor>.Filter.Eq(a => a.Id, actorId);
        var update = Builders<Actor>.Update
            .Set(a => a.Name, updatedActor.Name)
            .Set(a => a.MovieIds, currentMovieIds);

        await _actors.UpdateOneAsync(filter, update);

        // Step 4: Update movies by adding or removing the actor as necessary
        foreach (var movieId in moviesToAddActor)
        {
            await _movieService.AppendActorToMovieAsync(movieId.ToString(), updatedActor);
        }

        foreach (var movieId in moviesToRemoveActor)
        {
            await _movieService.RemoveActorFromMovieAsync(movieId.ToString(), actorId);
        }

        Console.WriteLine($"Actor with ID {actorId} updated successfully.");
    }

    // Delete an actor
    public async Task DeleteActorAsync(string actorId)
    {
        // Find the actor to delete and get associated MovieIds
        var actorToDelete = await _actors.Find(a => a.Id == actorId).FirstOrDefaultAsync();
        if (actorToDelete == null)
        {
            Console.WriteLine("Actor not found.");
            return;
        }

        // Check if MovieIds is null or empty before iterating
        if (actorToDelete.MovieIds != null && actorToDelete.MovieIds.Any())
        {
            // Remove the actor from the movies' ActorIds and ActorsInfo lists
            foreach (var movieId in actorToDelete.MovieIds)
            {
                var filter = Builders<Movie>.Filter.Eq(m => m.Id, movieId.ToString());

                // Ensure we remove the actor's ID from ActorIds and also from ActorsInfo if it exists
                var updateActorIds = Builders<Movie>.Update.Pull(m => m.ActorIds, new MongoDB.Bson.ObjectId(actorId));

                // Check if the movie document's ActorsInfo is initialized before pulling the actor
                var updateActorsInfo = Builders<Movie>.Update.PullFilter(
                    m => m.ActorsInfo,
                    Builders<Actor>.Filter.Eq(a => a.Id, actorId));

                // Apply both updates to remove the actor from ActorIds and ActorsInfo
                await _movies.UpdateOneAsync(filter, Builders<Movie>.Update.Combine(updateActorIds, updateActorsInfo));
            }
        }

        // Delete the actor from the actors collection
        await _actors.DeleteOneAsync(a => a.Id == actorId);

        Console.WriteLine($"Actor with ID {actorId} and all references to them have been removed successfully.");
    }






    public async Task RemoveMovieFromActorAsync(string actorId, ObjectId movieId)
    {
        if (string.IsNullOrEmpty(actorId))
        {
            Console.WriteLine("Actor ID is null or empty.");
            return;
        }

        // Build the filter to find the actor by actorId
        var filter = Builders<Actor>.Filter.Eq(a => a.Id, actorId);

        // Define the update operation to remove the specific movieId from the actor's MovieIds list
        var update = Builders<Actor>.Update.Pull(a => a.MovieIds, movieId);

        // Apply the update to the actor document
        var result = await _actors.UpdateOneAsync(filter, update);

        if (result.ModifiedCount > 0)
        {
            Console.WriteLine($"Movie with ID {movieId} removed from actor {actorId}.");
        }
        else
        {
            Console.WriteLine("No changes made. Either the actor does not exist or the movie was not associated with this actor.");
        }
    }



}
