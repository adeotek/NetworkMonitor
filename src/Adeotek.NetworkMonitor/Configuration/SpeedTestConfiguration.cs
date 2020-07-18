using System.Collections.Generic;

namespace Adeotek.NetworkMonitor.Configuration
{
    public class SpeedTestConfiguration
    {
        public bool Enabled { get; set; } = false;
        public int Requests { get; set; } = 1;
        public List<SpeedTestServer> Servers { get; set; }
        public string WriteToCollection { get; set; }
    }
}