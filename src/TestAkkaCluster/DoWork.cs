namespace TestAkkaClusterPerformance
{
    public class DoWork
    {
        public string EntityId { get; private set; }
        public int Shift { get;private set; }
        public bool IsWarmup { get;private set; }

        public DoWork(string entityId, int shift, bool isWarmup = false)
        {
            EntityId = entityId;
            Shift = shift;
            IsWarmup = isWarmup;
        }
    }
}
