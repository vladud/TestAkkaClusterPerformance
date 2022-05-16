using Akka.Actor;
using Akka.Event;
using System;
using System.Threading;

namespace TestAkkaClusterPerformance
{
    public class LazyWorker : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Logging.GetLogger(Context);

        public LazyWorker()
        {
            Receive<DoWork>((doWork) =>
            {
                if (doWork.IsWarmup)
                    _log.Info($"{doWork.EntityId}: I'm all warmed-up!");
                else
                    DoNothing(doWork);
            });
        }

        private void DoNothing(DoWork doWork)
        {
            var delayMs = doWork.Shift;
            _log.Info($"{doWork.EntityId}: Warking hard for {delayMs}ms");
            Thread.Sleep(TimeSpan.FromMilliseconds(delayMs));
        }
    }
}
