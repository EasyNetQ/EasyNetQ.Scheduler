﻿using System.Data.Common;
using log4net.Config;
using Microsoft.Data.SqlClient;
using Topshelf;

namespace EasyNetQ.Scheduler
{
    public class Program
    {
        static void Main()
        {
            XmlConfigurator.Configure();
            RegisterDbProviderFactories();

            HostFactory.Run(hostConfiguration =>
            {
                hostConfiguration.EnableServiceRecovery( serviceRecoveryConfiguration =>
                {
                    serviceRecoveryConfiguration.RestartService( delayInMinutes: 1 ); // On the first service failure, reset service after a minute
                    serviceRecoveryConfiguration.SetResetPeriod( days: 0 ); // Reset failure count after every failure
                } );
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

        private static void RegisterDbProviderFactories()
        {
            DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);
            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
            DbProviderFactories.RegisterFactory("Devart.Data.PostgreSql", Devart.Data.PostgreSql.PgSqlProviderFactory.Instance);
        }
    }
}
