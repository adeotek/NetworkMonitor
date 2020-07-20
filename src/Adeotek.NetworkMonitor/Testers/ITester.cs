using System.Collections.Generic;
using Adeotek.NetworkMonitor.Configuration;
using Adeotek.NetworkMonitor.Results;

namespace Adeotek.NetworkMonitor.Testers
{
    public interface ITester
    {
        void Run(TestConfiguration test);
        ICollection<ITestResult> RunAndReturn(TestConfiguration test);
    }
}