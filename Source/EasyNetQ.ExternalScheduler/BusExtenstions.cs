using EasyNetQ.DI;
using EasyNetQ.Scheduling;

namespace EasyNetQ
{
    public static class ServiceRegisterExtensions
    {
        public static IServiceRegister EnableExternalScheduler(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<IScheduler, ExternalScheduler.ExternalScheduler>();
        }
    }
}
