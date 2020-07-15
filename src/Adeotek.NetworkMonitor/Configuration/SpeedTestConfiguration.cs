using System.Collections.Generic;

namespace Adeotek.NetworkMonitor.Configuration
{
    public class SpeedTestConfiguration
    {
        public bool Enabled { get; set; } = false;
        public int Requests { get; set; } = 1;
        public List<SpeedTestServer> Servers { get; set; }
        public bool SendToGoogleSpreadsheet { get; set; } = false;
        public string GoogleSheetName { get; set; }
        public string GoogleSheetTimestampColumn { get; set; }
    }
}
