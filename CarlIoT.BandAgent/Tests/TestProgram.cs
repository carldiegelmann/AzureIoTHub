using CarlIoT.Common;
using NUnit.Framework;

namespace CarlIoT.BandAgent.Tests
{
    [TestFixture]
    public class TestProgram
    {
        [Test]
        public void Keypress()
        {
            var telemetry = new Telemetry
            {
                Status = StatusType.Emergency,
                Latitude = 123,
                Longitude = 234
            };

            var telemetryString = Program.SerializeToJson(telemetry);
            Assert.That(telemetryString, Is.EqualTo("{\"Longitude\":234.0,\"Latitude\":123.0,\"Status\":3}"));
        }
    }
}
