using System.Collections.Generic;

namespace Adeotek.NetworkMonitor.Configuration
{
    public class AppConfiguration
    {
        public PingConfiguration PingTest { get; set; }
        public SpeedTestConfiguration SpeedTest { get; set; }
        public List<DataTarget> WriteDataTo { get; set; }
        public string AppPath { get; set; }
    }
}
