using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CarlIoT.Common;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Newtonsoft.Json;

namespace CarlIoT.MessageProcessor
{
    class LoggingEventProcessor : IEventProcessor
    {
        public Task OpenAsync(PartitionContext context)
        {
            Console.WriteLine("LoggingEventProcessor opened, processing partition: " + $"'{context.PartitionId}'");
            return Task.CompletedTask;
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine("LoggingEventProcessor closing, processing partition: " + $"'{context.PartitionId}', reason: '{reason}'");
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Console.WriteLine("LoggingEventProcessor error, processing partition: " + $"'{context.PartitionId}', error: '{error}'");
            return Task.CompletedTask;
        }

        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            Console.WriteLine("Batch of events received on partition: " + $"'{context.PartitionId}'");
            foreach (var message in messages)
            {
                var payload = Encoding.ASCII.GetString(message.Body.Array,
                    message.Body.Offset,
                    message.Body.Count);
                var deviceId = message.SystemProperties["iothub-connection-device-id"];
                Console.WriteLine($"Message received on partition '{context.PartitionId}', "+
                                  $"device Id: '{deviceId}', "+
                                  $"payload: '{payload}'");
                var telemetry = JsonConvert.DeserializeObject<Telemetry>(payload);

                if (telemetry.Status == StatusType.Emergency)
                {
                    Console.WriteLine($"Guest requires emergency assistance! Device ID: {deviceId}");
                    SendFirstRespondersTo(telemetry.Latitude, telemetry.Longitude);
                }
            }
            
            return context.CheckpointAsync(); // setting checkpoints for getting only processed data
        }

        private void SendFirstRespondersTo(decimal telemetryLatitude, decimal telemetryLongitude)
        {
            Console.WriteLine($"** First responders dispatch to ({telemetryLatitude},{telemetryLongitude})");
        }
    }
}
