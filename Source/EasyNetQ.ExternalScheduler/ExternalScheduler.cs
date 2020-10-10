using System;
using System.Threading.Tasks;
using EasyNetQ.Producer;
using EasyNetQ.Scheduling;
using EasyNetQ.Topology;

namespace EasyNetQ.ExternalScheduler
{
    public class ExternalScheduler : IScheduler
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IExchangeDeclareStrategy exchangeDeclareStrategy;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly IMessageSerializationStrategy messageSerializationStrategy;
        private readonly ITypeNameSerializer typeNameSerializer;

        public ExternalScheduler(
            IAdvancedBus advancedBus,
            IConventions conventions,
            ITypeNameSerializer typeNameSerializer,
            IExchangeDeclareStrategy exchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IMessageSerializationStrategy messageSerializationStrategy
        )
        {
            this.advancedBus = advancedBus ?? throw new ArgumentNullException(nameof(advancedBus));
            this.conventions = conventions ?? throw new ArgumentNullException(nameof(conventions));
            this.typeNameSerializer = typeNameSerializer ?? throw new ArgumentNullException(nameof(typeNameSerializer));
            this.exchangeDeclareStrategy = exchangeDeclareStrategy ??
                                           throw new ArgumentNullException(nameof(exchangeDeclareStrategy));
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy ??
                                               throw new ArgumentNullException(nameof(messageDeliveryModeStrategy));
            this.messageSerializationStrategy = messageSerializationStrategy ??
                                                throw new ArgumentNullException(nameof(messageSerializationStrategy));
        }

        public async Task FuturePublishAsync<T>(DateTime futurePublishDate, T message, string cancellationKey = null)
            where T : class
        {
            await FuturePublishAsync(futurePublishDate, message, "#", cancellationKey);
        }

        public async Task FuturePublishAsync<T>(
            DateTime futurePublishDate, T message, string topic, string cancellationKey = null
        ) where T : class
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var scheduleMeType = typeof(ScheduleMe);
            var scheduleMeExchange = await exchangeDeclareStrategy
                .DeclareExchangeAsync(scheduleMeType, ExchangeType.Topic).ConfigureAwait(false);
            var baseMessageType = typeof(T);
            var concreteMessageType = message.GetType();
            var serializedMessage = messageSerializationStrategy.SerializeMessage(new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(concreteMessageType)
                }
            });
            var scheduleMe = new ScheduleMe
            {
                WakeTime = futurePublishDate,
                CancellationKey = cancellationKey,
                InnerMessage = serializedMessage.Body,
                MessageProperties = serializedMessage.Properties,
                BindingKey = typeNameSerializer.Serialize(typeof(T)),
                ExchangeType = ExchangeType.Topic,
                Exchange = conventions.ExchangeNamingConvention(baseMessageType),
                RoutingKey = topic
            };
            var easyNetQMessage = new Message<ScheduleMe>(scheduleMe)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(scheduleMeType)
                }
            };
            await advancedBus.PublishAsync(scheduleMeExchange, conventions.TopicNamingConvention(scheduleMeType), false,
                easyNetQMessage).ConfigureAwait(false);
        }


        public void FuturePublish<T>(DateTime futurePublishDate, T message, string cancellationKey = null)
            where T : class
        {
            FuturePublish(futurePublishDate, message, "#", cancellationKey);
        }

        public void FuturePublish<T>(DateTime futurePublishDate, T message, string topic, string cancellationKey = null)
            where T : class
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var scheduleMeType = typeof(ScheduleMe);
            var scheduleMeExchange = exchangeDeclareStrategy.DeclareExchange(scheduleMeType, ExchangeType.Topic);
            var baseMessageType = typeof(T);
            var concreteMessageType = message.GetType();
            var serializedMessage = messageSerializationStrategy.SerializeMessage(new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(concreteMessageType)
                }
            });
            var scheduleMe = new ScheduleMe
            {
                WakeTime = futurePublishDate,
                CancellationKey = cancellationKey,
                InnerMessage = serializedMessage.Body,
                MessageProperties = serializedMessage.Properties,
                BindingKey = typeNameSerializer.Serialize(typeof(T)),
                ExchangeType = ExchangeType.Topic,
                Exchange = conventions.ExchangeNamingConvention(baseMessageType),
                RoutingKey = topic
            };
            var easyNetQMessage = new Message<ScheduleMe>(scheduleMe)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(scheduleMeType)
                }
            };
            advancedBus.Publish(scheduleMeExchange, conventions.TopicNamingConvention(scheduleMeType), false,
                easyNetQMessage);
        }


        public Task FuturePublishAsync<T>(TimeSpan messageDelay, T message, string cancellationKey = null)
            where T : class
        {
            return FuturePublishAsync(messageDelay, message, "#", cancellationKey);
        }

        public Task FuturePublishAsync<T>(TimeSpan messageDelay, T message, string topic, string cancellationKey = null)
            where T : class
        {
            return FuturePublishAsync(DateTime.UtcNow.Add(messageDelay), message, topic, cancellationKey);
        }

        public void FuturePublish<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class
        {
            FuturePublish(messageDelay, message, "#", cancellationKey);
        }

        public void FuturePublish<T>(TimeSpan messageDelay, T message, string topic, string cancellationKey = null)
            where T : class
        {
            FuturePublish(DateTime.UtcNow.Add(messageDelay), message, topic, cancellationKey);
        }

        public async Task CancelFuturePublishAsync(string cancellationKey)
        {
            var unscheduleMeType = typeof(UnscheduleMe);
            var unscheduleMeExchange = await exchangeDeclareStrategy
                .DeclareExchangeAsync(unscheduleMeType, ExchangeType.Topic).ConfigureAwait(false);
            var unscheduleMe = new UnscheduleMe {CancellationKey = cancellationKey};
            var easyNetQMessage = new Message<UnscheduleMe>(unscheduleMe)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(unscheduleMeType)
                }
            };
            await advancedBus.PublishAsync(unscheduleMeExchange, conventions.TopicNamingConvention(unscheduleMeType),
                false, easyNetQMessage).ConfigureAwait(false);
        }

        public void CancelFuturePublish(string cancellationKey)
        {
            var unscheduleMeType = typeof(UnscheduleMe);
            var unscheduleMeExchange = exchangeDeclareStrategy.DeclareExchange(unscheduleMeType, ExchangeType.Topic);
            var unscheduleMe = new UnscheduleMe {CancellationKey = cancellationKey};
            var easyNetQMessage = new Message<UnscheduleMe>(unscheduleMe)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(unscheduleMeType)
                }
            };
            advancedBus.Publish(unscheduleMeExchange, conventions.TopicNamingConvention(unscheduleMeType), false,
                easyNetQMessage);
        }
    }
}
