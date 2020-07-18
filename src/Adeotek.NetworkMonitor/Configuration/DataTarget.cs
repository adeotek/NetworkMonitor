using System.Collections.Generic;

namespace Adeotek.NetworkMonitor.Configuration
{
    public class DataTarget
    {
        public string Type { get; set; }
        public Dictionary<string, string> Configuration { get; set; }
    }
}