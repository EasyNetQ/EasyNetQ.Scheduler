using System;
using System.Text;
using MongoDB.Driver;
using Xunit;

namespace EasyNetQ.Scheduler.Mongo.Tests
{
    public class ScheduleRepositoryTests : IClassFixture<SchedulesDatabaseFixture>
    {
        private readonly ScheduleRepository scheduleRepository;
        private readonly SchedulesDatabaseFixture databaseFixture;

        public ScheduleRepositoryTests(SchedulesDatabaseFixture databaseFixture)
        {
            this.databaseFixture = databaseFixture;
            scheduleRepository = new ScheduleRepository(databaseFixture.Configuration, () => DateTime.UtcNow);
        }

        [Fact]
        public void Should_be_able_to_store_a_schedule()
        {
            var schedule = new Schedule
            {
                BindingKey = "abc",
                CancellationKey = "bcd",
                WakeTime = new DateTime(2011, 5, 18),
                InnerMessage = Encoding.UTF8.GetBytes("Hello World!"),
                Id = Guid.NewGuid(),
                State = ScheduleState.Pending,
            };
            scheduleRepository.Store(schedule);

            var insertedDoc = databaseFixture.GetDocument(schedule.Id);

            Assert.Equal(schedule.Id, insertedDoc["_id"].AsGuid);
            Assert.Equal(schedule.CancellationKey, insertedDoc["CancellationKey"].AsString);
        }

        [Fact]
        public void Should_be_able_to_cancel_a_schedule()
        {
            var cancellation = Guid.NewGuid().ToString();
            databaseFixture.InitScheduleWithCancellation(cancellation);

            scheduleRepository.Cancel(cancellation);

            var count = databaseFixture.Collection.CountDocuments(d => d["CancellationKey"] == cancellation);
            Assert.Equal(0, count);
        }

        [Fact]
        public void Should_be_able_to_get_messages()
        {
            var id = Guid.NewGuid();
            var wakeTime = DateTime.MinValue.AddDays(5);
            databaseFixture.InitializeOne(id, ScheduleState.Pending, wakeTime);

            var schedule = scheduleRepository.GetPending();

            Assert.Equal(id, schedule.Id);
        }

        [Fact]
        public void Should_be_able_to_handle_timeout()
        {
            var id = Guid.NewGuid();
            var publishingTime = DateTime.UtcNow.Add(-databaseFixture.Configuration.PublishTimeout);
            databaseFixture.InitializeOne(id, ScheduleState.Publishing, publishingTime: publishingTime);

            scheduleRepository.HandleTimeout();

            var actual = databaseFixture.GetDocument(id);
            Assert.Equal(ScheduleState.Pending, actual["State"]);
        }


        [Fact]
        public void Should_be_able_to_mark_as_published()
        {
            var id = Guid.NewGuid();
            databaseFixture.InitializeOne(id);
            scheduleRepository.MarkAsPublished(id);

            var actual = databaseFixture.GetDocument(id);

            Assert.Equal(ScheduleState.Published, actual["State"]);
        }
    }
}

// ReSharper restore InconsistentNaming
