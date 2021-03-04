using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CarlIoT.Common;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Serilog;

namespace CarlIoT.BandAgent
{
    class Program
    {
        private static DeviceClient _device;
        private static TwinCollection _reportedProperties;

        private const string DeviceConnectionString =
            "HostName=lrac-demo-hub.azure-devices.net;DeviceId=device-01;SharedAccessKey=ZReyMAw3ytJfEhKFWaUqvzBqM1FXZ/NiNnNO5ctWglo=";
        
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext().WriteTo
                .Console().WriteTo
                .Seq("http://localhost:5341/")
                .CreateLogger();

            Log.Information("Starting up");

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.WriteLine("Init Band Agent...");

            _device = DeviceClient.CreateFromConnectionString(DeviceConnectionString);

            await _device.OpenAsync(cts.Token); // connect to azure hub

            //var foo = Task.Run(() => ReceiveEvents(device));
            var receiveEvents = ReceiveEvents(_device);

            await _device.SetMethodHandlerAsync("showMessage", ShowMessage, null, cts.Token);

            Console.WriteLine("Device is connected!");

            await UpdateTwin(_device);
            await _device.SetDesiredPropertyUpdateCallbackAsync(UpdateProperties, null);

            Console.WriteLine("Press a key to perform an action:");
            Console.WriteLine("ESC: quits");
            Console.WriteLine("h: sends a happy feedback");
            Console.WriteLine("u: sends a unhappy feedback");
            Console.WriteLine("e: requests emergency help");

            var consoleKeyTask = Task.Run(() => { CheckKeypress(_device, cts.Token); }, cts.Token);

            await consoleKeyTask;
        }

        public static async Task CheckKeypress(DeviceClient deviceClient, CancellationToken cancellationToken)
        {
            ConsoleKeyInfo cki = new ConsoleKeyInfo();
            var random = new Random();
            do
            {
                cki = Console.ReadKey();
                    Console.Write("Action? ");
                    //var input = Console.ReadKey().KeyChar;
                    Console.WriteLine();

                    var status = StatusType.NotSpecified;
                    var latitude = random.Next(0, 100);
                    var longitude = random.Next(0, 100);

                    switch (Char.ToLower(cki.KeyChar))
                    {
                        case 'h':
                            status = StatusType.Happy;
                            break;
                        case 'u':
                            status = StatusType.Unhappy;
                            break;
                        case 'e':
                            status = StatusType.Emergency;
                            break;
                        default:
                            break;
                    }

                    var telemetry = new Telemetry
                    {
                        Status = status,
                        Latitude = latitude,
                        Longitude = longitude
                    };

                    var payload = JsonConvert.SerializeObject(telemetry);

                    var message = new Message(Encoding.ASCII.GetBytes(payload));

                    await deviceClient.SendEventAsync(message, cancellationToken);

                    Console.WriteLine("Message was sent successfully!! :)");

                    // Wait for an ESC
            } while (cki.Key != ConsoleKey.Escape);
        }

        private static Task<MethodResponse> ShowMessage(
            MethodRequest methodrequest, 
            object usercontext)
        {
            Console.WriteLine(" *** MESSAGE RECEIVED ***");
            Console.WriteLine(methodrequest.DataAsJson);

            var responsePayload = Encoding.ASCII.GetBytes("{\"response\": \"Message shown!\"}");
            return Task.FromResult(new MethodResponse(responsePayload, 200));
        }

        private static async Task<CancellationToken> ReceiveEvents(DeviceClient device)
        {
            while (true)
            {
                var message = await device.ReceiveAsync();

                if (message == null)
                {
                    continue; // skip i
                }

                var messageBody = message.GetBytes();
                var payload = Encoding.ASCII.GetString(messageBody);
                Console.WriteLine($"Received message from cloud: '{payload}'");

                await device.CompleteAsync(message);
            }
        }

        private static async Task UpdateTwin(DeviceClient device)
        {
            _reportedProperties = new TwinCollection();
            _reportedProperties["firmwareVersion"] = "1.0";
            _reportedProperties["firmwareUpdateStatus"] = "n/a";

            await device.UpdateReportedPropertiesAsync(_reportedProperties);
        }

        private static Task UpdateProperties(TwinCollection desiredProperties, object usercontext)
        {
            var currentFirmwareVersion = (string) _reportedProperties["firmwareVersion"];
            var desiredFirmwareVersion = (string) desiredProperties["firmwareVersion"];

            if (currentFirmwareVersion != desiredFirmwareVersion)
            {
                Console.WriteLine($"Firmware update requested. Current Version: '{currentFirmwareVersion} requested Version: '{desiredFirmwareVersion}");
                ApplyFirmwareUpdate(desiredFirmwareVersion);
            }
            return Task.CompletedTask;
        }

        private static async Task ApplyFirmwareUpdate(string targetVersion)
        {
            Console.WriteLine("Beginning firmware update...");

            _reportedProperties["firmwareUpdateStatus"] = $"Downloading zip file for firmware {targetVersion}...";
            await _device.UpdateReportedPropertiesAsync(_reportedProperties);
            Thread.Sleep(5000);

            _reportedProperties["firmwareUpdateStatus"] = $"Unzipping package...";
            await _device.UpdateReportedPropertiesAsync(_reportedProperties);
            Thread.Sleep(5000);

            _reportedProperties["firmwareUpdateStatus"] = $"Applying update...";
            await _device.UpdateReportedPropertiesAsync(_reportedProperties);
            Thread.Sleep(5000);

            Console.WriteLine("Firmware update complete!");

            _reportedProperties["firmwareUpdateStatus"] = "n/a";
            _reportedProperties["firmwareVersion"] = targetVersion;
            await _device.UpdateReportedPropertiesAsync(_reportedProperties);
        }
    }
}
