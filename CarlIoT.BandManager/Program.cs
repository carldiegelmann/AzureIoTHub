using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace CarlIoT.BandManager
{
    class Program
    {
        static async Task Main(string[] args) {

            var serviceConnectionString = "HostName=lrac-demo-hub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=BkNHrdM+H5c8v5BRm0uWxnFH32dk+yO1syccKHXtT2w=";

            var serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString);
            var registryManager = RegistryManager.CreateFromConnectionString(serviceConnectionString);



            ReveiveFeedback(serviceClient);

            while (true)
            {
                Console.WriteLine("Which device do you wish to send a message to? ");
                Console.Write("> ");
                var deviceId = Console.ReadLine();

                //await SendCloudToDeviceMessage(serviceClient, deviceId);
                //await CallDirectMethod(serviceClient, deviceId);
                await UpdateDeviceFirmware(registryManager, deviceId);
            }
        }

        private static async Task CallDirectMethod(ServiceClient serviceClient, string deviceId)
        {
            var method = new CloudToDeviceMethod("showMessage");
            method.SetPayloadJson("'Hello from C#'");

            var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, method);

            Console.WriteLine($"Response status: {response.Status}, payload: {response.GetPayloadAsJson()}");

        }

        private static async Task SendCloudToDeviceMessage(ServiceClient serviceClient, string deviceId)
        {
            Console.WriteLine("What payload do you want to send? ");
            Console.Write("> ");
            var payload = Console.ReadLine();

            var commandMessage = new Message(Encoding.ASCII.GetBytes(payload));
            commandMessage.MessageId = Guid.NewGuid().ToString();
            commandMessage.Ack = DeliveryAcknowledgement.Full;
            commandMessage.ExpiryTimeUtc = DateTime.UtcNow.AddSeconds(10);

            await serviceClient.SendAsync(deviceId, commandMessage);
        }

        private static async Task ReveiveFeedback(ServiceClient serviceClient)
        {

            var feedbackReceiver = serviceClient.GetFeedbackReceiver();

            while (true)
            {
                var feedbackBatch = await feedbackReceiver.ReceiveAsync();

                if (feedbackBatch == null)
                {
                    continue;
                }

                foreach (var record in feedbackBatch.Records)
                {
                    var messageId = record.OriginalMessageId;
                    var statusCode = record.StatusCode;

                    Console.WriteLine($"Feedback for message '{messageId}', status code: '{statusCode}");
                }
                
            }
        }

        private static async Task UpdateDeviceFirmware(RegistryManager registryManager, string deviceId)
        {
            var deviceTwin = await registryManager.GetTwinAsync(deviceId);

            var twinPatch = new
            {
                properties = new
                {
                    desired = new
                    {
                        firmwareVersion = "2.0"
                    }
                }
            };

            var twinPatchJson = JsonConvert.SerializeObject(twinPatch);


            await registryManager.UpdateTwinAsync(deviceId, twinPatchJson, deviceTwin.ETag);

            Console.WriteLine($"Firmware update sent to device '{deviceId}'...");

            while (true)
            {
                Thread.Sleep(1000);

                deviceTwin = await registryManager.GetTwinAsync(deviceId);

                Console.WriteLine($"Firmware update status: {deviceTwin.Properties.Reported}");

                if (deviceTwin.Properties.Reported["firmwareVersion"] == "2.0")
                {
                    Console.WriteLine("Firmware update complete!");
                    break;
                }
            }
        }

    }
}
