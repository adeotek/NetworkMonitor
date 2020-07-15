using System.Net.NetworkInformation;

namespace Adeotek.NetworkMonitor
{
    public class PingResult
    {
        public string Target { get; set; }
        public bool Success { get; set; }
        public long Time { get; set; }
        public string Message { get; set; }
        public string Address { get; set; }

        public PingResult(string target = null)
        {
            Target = target;
            Success = false;
            Time = -1;
        }

        public PingResult(PingReply reply, string target = null)
        {
            Target = target;
            if (reply == null)
            {
                Success = false;
                Time = -1;
                Message = "No reply received";
                return;
            }

            if (reply.Status != IPStatus.Success)
            {
                Success = false;
                Time = -1;
                Message = reply.Status.ToString();
            }
            else
            {
                Success = true;
                Time = reply.RoundtripTime;
            }
            Address = reply.Address?.ToString();
        }
    }
}
