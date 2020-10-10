using System;

namespace EasyNetQ.Scheduler.Mongo
{
    public interface IScheduleRepositoryConfiguration
    {
        string ConnectionString { get; }
        string DatabaseName { get; }
        string CollectionName { get; }
        TimeSpan DeleteTimeout { get; }
        TimeSpan PublishTimeout { get; }
    }
}
