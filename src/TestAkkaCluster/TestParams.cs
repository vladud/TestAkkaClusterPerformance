using Microsoft.Extensions.Configuration;

namespace TestAkkaClusterPerformance
{
    public class TestParams
    {
        public TestParams(IConfiguration configuration)
        {
            NoOfWorkers = int.Parse(configuration["NoOfWorkers"]);
            DoWarmup = bool.Parse(configuration["DoWarmup"]);
            ShiftInMs = int.Parse(configuration["ShiftInMs"]);
            SendInBatches = bool.Parse(configuration["SendInBatches"]);
            BatchSize = int.Parse(configuration["BatchSize"]);
            BatchDelayInMs = int.Parse(configuration["BatchDelayInMs"]);
            StartAfterInSeconds = int.Parse(configuration["StartAfterInSeconds"]);
        }

        public int NoOfWorkers { get; set; }
        public bool DoWarmup { get; set; }
        public int ShiftInMs { get; set; }
        public bool SendInBatches { get; set; }
        public int BatchSize { get; set; }
        public int BatchDelayInMs { get; set; }
        public int StartAfterInSeconds { get; set; }
    }
}
