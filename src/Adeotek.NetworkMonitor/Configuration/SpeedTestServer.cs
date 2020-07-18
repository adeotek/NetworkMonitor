namespace Adeotek.NetworkMonitor.Configuration
{
    public class SpeedTestServer
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public int[] Filter { get; set; }
    }
}