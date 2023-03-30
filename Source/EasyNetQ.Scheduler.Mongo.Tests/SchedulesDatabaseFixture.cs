using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace EasyNetQ.Scheduler.Mongo.Tests
{

    public class SchedulesDatabaseFixture : IDisposable
    {
        public readonly IMongoCollection<BsonDocument> Collection;
        private readonly IMongoDatabase database;

        public readonly ScheduleRepositoryConfiguration Configuration = new ScheduleRepositoryConfiguration
        {
            ConnectionString = "mongodb://localhost:27017/?w=1&readPreference=primary&uuidRepresentation=csharpLegacy",
            CollectionName = "Schedules",
            DatabaseName = "EasyNetQ",
            DeleteTimeout = TimeSpan.FromMinutes(5),
            PublishTimeout = TimeSpan.FromSeconds(60),
        };

        public SchedulesDatabaseFixture()
        {
            InitializeMongoConventions();
            database = CreateDatabase(new MongoUrl(Configuration.ConnectionString), Configuration.DatabaseName);
            Collection = database.GetCollection<BsonDocument>(Configuration.CollectionName);
        }

        public void Dispose()
        {

        }

        public void InitScheduleWithCancellation(string cancellation)
        {
            var schedule = new Schedule
            {
                CancellationKey = cancellation,
                Id = Guid.NewGuid()
            };
            Collection.InsertOne(schedule.ToBsonDocument());
        }

        public void InitializeOne(Guid id, ScheduleState state = ScheduleState.Pending, DateTime? wakeTime = null, DateTime? publishingTime = null)
        {
            var schedule = new Schedule
            {
                Id = id,
                State = state,
                WakeTime = wakeTime ?? DateTime.UtcNow.AddDays(-1),
                PublishingTime = publishingTime
            };
            Collection.InsertOne(schedule.ToBsonDocument());
        }

        public BsonDocument GetDocument(Guid id)
            => database.GetCollection<BsonDocument>(Configuration.CollectionName)
                .Find(d => d["_id"] == id)
                .FirstOrDefault();

        private static IEnumerable<BsonDocument> LoadBSONs()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BSONs");
            foreach (var fileName in Directory.GetFiles(path))
            {
                var content = File.ReadAllText(fileName);
                var bson = BsonSerializer.Deserialize<BsonDocument>(content);
                bson["_id"] = Guid.NewGuid();
                yield return  bson;
            }
        }

        private static IMongoDatabase CreateDatabase(MongoUrl connectionString, string databaseName)
        {
            var settings = MongoClientSettings.FromUrl(connectionString);
            settings.ReadEncoding = new UTF8Encoding(false, false);
            var client = new MongoClient(settings);
            return client.GetDatabase(databaseName);
        }

        private static void InitializeMongoConventions()
        {
            ConventionRegistry.Register("ignoreIfNull", new ConventionPack {new IgnoreIfNullConvention(true)}, t => true);
            ConventionRegistry.Register("ignoreExtraElements", new ConventionPack {new IgnoreExtraElementsConvention(true)}, t => true);
        }
    }
}
