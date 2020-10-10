using System;
using EasyNetQ.Scheduler.Mongo;

namespace EasyNetQ.Scheduler.Mongo.Tests
{
    public class MockScheduleRepository : IScheduleRepository
    {
        public Func<Schedule> GetPendingDelegate { get; set; }

        public void Store(Schedule scheduleMe)
        {
        }

        public void Cancel(string cancellation)
        {
        }

        public Schedule GetPending()
        {
            return (GetPendingDelegate != null)
                       ? GetPendingDelegate()
                       : null;
        }

        public void MarkAsPublished(Guid id)
        {
        }

        public void HandleTimeout()
        {
        }
    }
}
