﻿using log4net.Config;
using MongoDB.Bson.Serialization.Conventions;
using Topshelf;

namespace EasyNetQ.Scheduler.Mongo
{
    public class Program
    {
        private static void Main()
        {
            XmlConfigurator.Configure();
            InitializeMongoConventions();

            HostFactory.Run(hostConfiguration =>
            {
                hostConfiguration.EnableServiceRecovery(serviceRecoveryConfiguration =>
                {
                    serviceRecoveryConfiguration
                        .RestartService(1); // On the first service failure, reset service after a minute
                    serviceRecoveryConfiguration.SetResetPeriod(0); // Reset failure count after every failure
                });
                hostConfiguration.RunAsLocalSystem();
                hostConfiguration.SetDescription("EasyNetQ.Scheduler");
                hostConfiguration.SetDisplayName("EasyNetQ.Scheduler");
                hostConfiguration.SetServiceName("EasyNetQ.Scheduler");

                hostConfiguration.Service<ISchedulerService>(serviceConfiguration =>
                {
                    serviceConfiguration.ConstructUsing(_ => SchedulerServiceFactory.CreateScheduler());

                    serviceConfiguration.WhenStarted((service, _) =>
                    {
                        service.Start();
                        return true;
                    });
                    serviceConfiguration.WhenStopped((service, _) =>
                    {
                        service.Stop();
                        return true;
                    });
                });
            });
        }

        private static void InitializeMongoConventions()
        {
            ConventionRegistry.Register("ignoreIfNull", new ConventionPack {new IgnoreIfNullConvention(true)}, t => true);
            ConventionRegistry.Register("ignoreExtraElements", new ConventionPack {new IgnoreExtraElementsConvention(true)}, t => true);
        }
    }
}
