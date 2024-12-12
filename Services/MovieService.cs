using Labb4.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

public class MovieService
{
    private readonly IMongoCollection<Movie> _movies;
    private readonly IMongoCollection<Actor> _actors;

    public MovieService(IOptions<DatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(databaseSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseSettings.Value.DatabaseName);

        _movies = mongoDatabase.GetCollection<Movie>(databaseSettings.Value.MoviesCollectionName);
        _actors = mongoDatabase.GetCollection<Actor>(databaseSettings.Value.ActorsCollectionName);
    }
    

    public async Task<List<Movie>> GetMoviesWithActorsInfo()
    {
        return await _movies.Aggregate()
            .Lookup(
                _actors,
                movie => movie.ActorIds,
                actor => actor.Id,
                (Movie movie) => movie.ActorsInfo
            ).ToListAsync();
    }

    public async Task<Movie> GetMovieByIdAsync(string movieId)
    {
        // Fetch the movie based on its ID
        var filter = Builders<Movie>.Filter.Eq(m => m.Id, movieId);
        var movie = await _movies.Find(filter).FirstOrDefaultAsync();

        if (movie != null)
        {
            // Convert actorIds from ObjectId to string to avoid using ObjectId conversion in LINQ
            var actorIdStrings = movie.ActorIds?.Select(id => id.ToString()).ToList() ?? new List<string>();

            // Retrieve actors by comparing string representations of IDs
            movie.ActorsInfo = await _actors
                .Find(a => actorIdStrings.Contains(a.Id))
                .ToListAsync();
        }
        
        return movie;
    }

    public async Task CreateMovieAsync(Movie movie) => await _movies.InsertOneAsync(movie);


    public async Task UpdateMovieAsync(string id, Movie updatedMovie)
    {
        var filter = Builders<Movie>.Filter.Eq(m => m.Id, id);

        // Update the title and actor IDs
        var update = Builders<Movie>.Update
            .Set(m => m.Title, updatedMovie.Title)
            .Set(m => m.ActorIds, updatedMovie.ActorIds);

        await _movies.UpdateOneAsync(filter, update);

        // Retrieve the updated actor information based on the new ActorIds
        var actorIdStrings = updatedMovie.ActorIds.Select(id => id.ToString()).ToList();
        updatedMovie.ActorsInfo = await _actors
            .Find(a => actorIdStrings.Contains(a.Id))
            .ToListAsync();

        // Update the ActorsInfo in the movie document
        var updateActorsInfo = Builders<Movie>.Update
            .Set(m => m.ActorsInfo, updatedMovie.ActorsInfo);
        await _movies.UpdateOneAsync(filter, updateActorsInfo);
    }

    public async Task UpdateMovieActorsAsync(string movieId, List<ObjectId> actorIds, List<Actor> actorsInfo)
    {
        var filter = Builders<Movie>.Filter.Eq(m => m.Id, movieId);
        var update = Builders<Movie>.Update
            .Set(m => m.ActorIds, actorIds)
            .Set(m => m.ActorsInfo, actorsInfo);

        await _movies.UpdateOneAsync(filter, update);
    }


    public async Task AppendActorToMovieAsync(string movieId, Actor newActor)
    {
        var filter = Builders<Movie>.Filter.Eq(m => m.Id, movieId);

        // Fetch the current movie document
        var movie = await GetMovieByIdAsync(movieId);

        // Ensure ActorIds list contains the new actor's ID
        if (movie.ActorIds == null)
            movie.ActorIds = new List<ObjectId>();

        var newActorId = new ObjectId(newActor.Id);
        if (!movie.ActorIds.Contains(newActorId))
        {
            movie.ActorIds.Add(newActorId);
        }

        // Ensure ActorsInfo list contains the new actor's details
        if (movie.ActorsInfo == null)
            movie.ActorsInfo = new List<Actor>();

        if (!movie.ActorsInfo.Any(a => a.Id == newActor.Id))
        {
            movie.ActorsInfo.Add(new Actor
            {
                Id = newActor.Id,
                Name = newActor.Name,
                MovieIds = newActor.MovieIds
            });
        }

        // Update the movie with modified ActorIds and ActorsInfo without overwriting existing data
        var update = Builders<Movie>.Update
            .Set(m => m.ActorIds, movie.ActorIds)
            .Set(m => m.ActorsInfo, movie.ActorsInfo);

        await _movies.UpdateOneAsync(filter, update);
    }


    public async Task DeleteMovieAsync(string movieId)
    {
        // Find the movie to delete and get associated ActorIds
        var movieToDelete = await _movies.Find(m => m.Id == movieId).FirstOrDefaultAsync();
        if (movieToDelete == null)
        {
            Console.WriteLine("Movie not found.");
            return;
        }

        // Remove the movie from the actors' MovieIds list
        foreach (var actorId in movieToDelete.ActorIds)
        {
            var filter = Builders<Actor>.Filter.Eq(a => a.Id, actorId.ToString());
            var update = Builders<Actor>.Update.Pull(a => a.MovieIds, new MongoDB.Bson.ObjectId(movieId));
            await _actors.UpdateOneAsync(filter, update);
        }

        // Delete the movie from the movies collection
        await _movies.DeleteOneAsync(m => m.Id == movieId);

        

    }


    public async Task RemoveActorFromMovieAsync(string movieId, string actorId)
    {
        var filter = Builders<Movie>.Filter.Eq(m => m.Id, movieId);

        // Remove the actor's ObjectId from ActorIds and details from ActorsInfo
        var updateActorIds = Builders<Movie>.Update.Pull(m => m.ActorIds, new ObjectId(actorId));
        var updateActorsInfo = Builders<Movie>.Update.PullFilter(
            m => m.ActorsInfo,
            Builders<Actor>.Filter.Eq(a => a.Id, actorId));

        // Apply both updates to remove the actor from ActorIds and ActorsInfo
        await _movies.UpdateOneAsync(filter, Builders<Movie>.Update.Combine(updateActorIds, updateActorsInfo));
    }

}
