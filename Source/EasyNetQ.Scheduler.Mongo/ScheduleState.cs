namespace EasyNetQ.Scheduler.Mongo
{
    public enum ScheduleState
    {
        Unknown = 0,
        Pending = 1,
        Publishing = 2,
        Published = 3
    }
}
