using System;

namespace EasyNetQ.Scheduler.Mongo
{
    public interface ISchedulerServiceConfiguration
    {
        string SubscriptionId { get; }
        TimeSpan PublishInterval { get; }
        TimeSpan HandleTimeoutInterval { get; }
        int PublishMaxSchedules { get; }
    }
}
