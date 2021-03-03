using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;

namespace CarlIoT.MessageProcessor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var hubName = "lrac-demo-hub";
            var iotHubConnectionString = "Endpoint=sb://iothub-ns-lrac-demo-8392110-175616f6ce.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=6n6n+jLumqyoy/6gQ0x9FRDVqY2EcrG+43Z3PiXrx30=;EntityPath=lrac-demo-hub";
            var storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=carlpsdemostorage01;AccountKey=rKwiYFzVF7UQ1lvcAaUyl/QRDausrE2VWm0p5McdxIzi7/2I/dA/4CvbscqlGkvMTgy1SEMrJvMUDL1txHTUjQ==;EndpointSuffix=core.windows.net";
            var storageContainerName = "message-processor-host";
            var consumerGroupName = PartitionReceiver.DefaultConsumerGroupName;

            var processor = new EventProcessorHost(
                hubName,
                consumerGroupName,
                iotHubConnectionString,
                storageConnectionString,
                storageContainerName);

            await processor.RegisterEventProcessorAsync<LoggingEventProcessor>();

            Console.WriteLine("Event Processor start");

            Console.ReadLine();

            await processor.UnregisterEventProcessorAsync();
        }
    }

}
