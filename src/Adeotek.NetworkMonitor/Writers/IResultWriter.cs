using System.Collections.Generic;
using Adeotek.NetworkMonitor.Results;

namespace Adeotek.NetworkMonitor.Writers
{
    public interface IResultWriter
    {
        void WriteResults(ICollection<ITestResult> data, string collection, string group);
    }
}