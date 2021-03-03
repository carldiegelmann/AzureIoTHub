using System;
using System.Collections.Generic;
using System.Text;

namespace CarlIoT.Common
{
    public class Telemetry
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public StatusType Status { get; set; }
    }
}
