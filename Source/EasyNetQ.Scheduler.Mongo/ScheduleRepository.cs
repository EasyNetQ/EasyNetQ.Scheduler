using System;
using System.Text;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace EasyNetQ.Scheduler.Mongo
{
    public interface IScheduleRepository
    {
        void Store(Schedule schedule);
        void Cancel(string cancellation);
        Schedule GetPending();
        void MarkAsPublished(Guid id);
        void HandleTimeout();
    }

    public class ScheduleRepository : IScheduleRepository
    {
        private readonly IScheduleRepositoryConfiguration configuration;
        private readonly Func<DateTime> getNow;
        private readonly Lazy<IMongoCollection<Schedule>> lazyCollection;

        public ScheduleRepository(IScheduleRepositoryConfiguration configuration, Func<DateTime> getNow)
        {
            this.configuration = configuration;
            this.getNow = getNow;
            lazyCollection = new Lazy<IMongoCollection<Schedule>>(CreateAndIndex);
        }

        private IMongoCollection<Schedule> Collection => lazyCollection.Value;

        public void Store(Schedule schedule)
        {
            Collection.InsertOne(schedule);
        }

        public void Cancel(string cancellation)
        {
            Collection.DeleteOne(x => x.CancellationKey == cancellation);
        }

        public Schedule GetPending()
        {
            var now = getNow();
            var filter = Builders<Schedule>.Filter;
            var query = filter.And(
                filter.Eq(x => x.State, ScheduleState.Pending),
                filter.Lte(x => x.WakeTime, now));
            var update = Builders<Schedule>.Update
                .Set(x => x.State, ScheduleState.Publishing)
                .Set(x => x.PublishingTime, now);;
            var options = new FindOneAndUpdateOptions<Schedule> { Sort = Builders<Schedule>.Sort.Ascending(x => x.WakeTime), ReturnDocument = ReturnDocument.After };
            var findAndModifyResult = Collection.FindOneAndUpdate(query, update, options);

            return findAndModifyResult;
        }

        public void MarkAsPublished(Guid id)
        {
            var now = getNow();
            var update = Builders<Schedule>.Update
                    .Set(x => x.State, ScheduleState.Published)
                    .Set(x => x.PublishedTime, now)
                    .Unset(x => x.PublishingTime);
            Collection.UpdateOne(x => x.Id == id, update);
        }

        public void HandleTimeout()
        {
            var publishingTimeTimeout = getNow() - configuration.PublishTimeout;
            var filter = Builders<Schedule>.Filter;
            var query = filter.And(filter.Eq(x => x.State, ScheduleState.Publishing),
                filter.Lte(x => x.PublishingTime, publishingTimeTimeout));
            var update = Builders<Schedule>.Update
                .Set(x => x.State, ScheduleState.Pending)
                .Unset(x => x.PublishingTime);
            Collection.UpdateMany(query, update);
        }

        private IMongoCollection<Schedule> CreateAndIndex()
        {
            InitializeMongoConventions();
            var collection = GetICollection<Schedule>(configuration.ConnectionString, configuration.DatabaseName, configuration.CollectionName);
            collection.Indexes.CreateOne(new CreateIndexModel<Schedule>(
                Builders<Schedule>.IndexKeys.Ascending(x => x.CancellationKey),
                new CreateIndexOptions { Sparse = true }));
            collection.Indexes.CreateOne(new CreateIndexModel<Schedule>(
                Builders<Schedule>.IndexKeys
                    .Ascending(x => x.State)
                    .Ascending(x => x.WakeTime)));

            collection.Indexes.CreateOne(new CreateIndexModel<Schedule>(
                Builders<Schedule>.IndexKeys.Ascending(x => x.PublishedTime),
                new CreateIndexOptions
                {
                    Sparse = true,
                    ExpireAfter = configuration.DeleteTimeout
                }));

            return collection;
        }

        private static IMongoDatabase CreateDatabase(MongoUrl connectionString, string databaseName)
        {
            var settings = MongoClientSettings.FromUrl(connectionString);
            settings.ReadEncoding = new UTF8Encoding(false, false);
            var client = new MongoClient(settings);
            return client.GetDatabase(databaseName);
        }

        private static IMongoCollection<TDocument> GetICollection<TDocument>(string connectionString, string databaseName, string collectionName)
        {
            var database = CreateDatabase(new MongoUrl(connectionString), databaseName);
            return database.GetCollection<TDocument>(collectionName);
        }

        private static void InitializeMongoConventions()
        {
            ConventionRegistry.Register("ignoreIfNull", new ConventionPack {new IgnoreIfNullConvention(true)}, t => true);
            ConventionRegistry.Register("ignoreExtraElements", new ConventionPack {new IgnoreExtraElementsConvention(true)}, t => true);
        }
    }
}
