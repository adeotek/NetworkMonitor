using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Adeotek.NetworkMonitor
{
    public class Pinger
    {
        private readonly Ping _ping;

        public Pinger()
        {
            _ping = new Ping();
            PingOptions = new PingOptions
            {
                // Use the default TTL value which is 128,
                // but change the fragmentation behavior.
                DontFragment = true
            };
            Data = Encoding.ASCII.GetBytes("a quick brown fox jumped over the lazy dog");
        }

        public PingOptions PingOptions { get; set; }
        public byte[] Data { get; set; }
        public int Timeout { get; set; } = 120;

        public PingResult Ping(string host)
        {
            return new PingResult(_ping.Send(host, Timeout, Data, PingOptions), host);
        }

        public ITestResult SafePing(string host)
        {
            PingResult result;
            try
            {
                result = new PingResult(_ping.Send(host, Timeout, Data, PingOptions), host);
            }
            catch (Exception e)
            {
                result = new PingResult {Success = false, Target = host, Message = e.Message};
            }

            return result;
        }

        public async Task<ITestResult> PingAsync(string host)
        {
            var reply = await _ping.SendPingAsync(host, Timeout, Data, PingOptions);
            return new PingResult(reply, host);
        }

        public static ITestResult SendPing(string host)
        {
            return new Pinger().Ping(host);
        }
    }
}