using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Configuration;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System;
using Akka.DependencyInjection;
using Akka.Cluster.Sharding;
using Akka.Cluster;
using Akka.Util;
using System.Linq;
using Akka.Cluster.Tools.Singleton;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace TestAkkaClusterPerformance
{
    /// <summary>
    /// <see cref="IHostedService"/> that runs and manages <see cref="ActorSystem"/> in background of application.
    /// </summary>
    public class AkkaService : IHostedService
    {
        private ActorSystem ClusterSystem;
        private readonly IServiceProvider _serviceProvider;

        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly TestParams _testParams;
        private static ILogger Logger = Program.LogFactory.CreateLogger("AkkaService");

        public AkkaService(IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime, TestParams testParams)
        {
            _serviceProvider = serviceProvider;
            _applicationLifetime = appLifetime;
            _testParams = testParams;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
             var config = ConfigurationFactory.ParseString(File.ReadAllText("app.conf")).BootstrapFromDocker();
             var bootstrap = BootstrapSetup.Create()
                .WithConfig(config) // load HOCON
                .WithConfigFallback(ClusterSingletonManager.DefaultConfig())
                .WithConfigFallback(ClusterSharding.DefaultConfig())
                .WithActorRefProvider(ProviderSelection.Cluster.Instance); // launch Akka.Cluster

            // N.B. `WithActorRefProvider` isn't actually needed here - the HOCON file already specifies Akka.Cluster

            // enable DI support inside this ActorSystem, if needed
            var diSetup = DependencyResolverSetup.Create(_serviceProvider);

            // merge this setup (and any others) together into ActorSystemSetup
            var actorSystemSetup = bootstrap.And(diSetup);

            // start ActorSystem
            ClusterSystem = ActorSystem.Create("ClusterSys", actorSystemSetup);

            // start Petabridge.Cmd (https://cmd.petabridge.com/)
            var pbm = PetabridgeCmd.Get(ClusterSystem);
            pbm.RegisterCommandPalette(ClusterCommands.Instance);
            pbm.RegisterCommandPalette(new RemoteCommands());
            pbm.Start(); // begin listening for PBM management commands

            // instantiate actors
            var sharding = ClusterSharding.Get(ClusterSystem);
            var shardRegion = await sharding.StartAsync(
                typeName: "lazyWorker",
                entityPropsFactory: e => Props.Create(() => new LazyWorker()),
                settings: ClusterShardingSettings.Create(ClusterSystem),
                messageExtractor: new MessageExtractor(10));

            // use the ServiceProvider ActorSystem Extension to start DI'd actors
            var sp = DependencyResolver.For(ClusterSystem);
            // add a continuation task that will guarantee 
            // shutdown of application if ActorSystem terminates first
            _ = ClusterSystem.WhenTerminated.ContinueWith(tr => {
                _applicationLifetime.StopApplication();
            });

            var cluster = Cluster.Get(ClusterSystem);
            cluster.RegisterOnMemberUp(() =>
            {
                ProduceMessages(ClusterSystem, shardRegion);
            });

            await Task.CompletedTask;
        }

        private void ProduceMessages(ActorSystem system, IActorRef shardRegion)
        {
            var entityIds = Enumerable.Range(1, _testParams.NoOfWorkers).Select(e => e.ToString()).ToArray();

            if (_testParams.DoWarmup)
                foreach (var entityId in entityIds)
                {
                    shardRegion.Tell(new ShardEnvelope(entityId, new DoWork(entityId, 1, true)));
                }

            system.Scheduler.Advanced.ScheduleOnce(TimeSpan.FromSeconds(_testParams.StartAfterInSeconds), () =>
            {
                if (_testParams.SendInBatches)
                    SendToWorkInBatches(shardRegion, entityIds);
                else
                    SendToWork(shardRegion, entityIds);
            });
        }

        private void SendToWork(IActorRef shardRegion, string[] entityIds)
        {
            foreach (var entityId in entityIds)
            {
                shardRegion.Tell(new ShardEnvelope(entityId, new DoWork(entityId, _testParams.ShiftInMs)));
            }
        }

        private void SendToWorkInBatches(IActorRef shardRegion, string[] entityIds)
        {
            var i = 0;
            foreach (var entityId in entityIds)
            {
                shardRegion.Tell(new ShardEnvelope(entityId, new DoWork(entityId, _testParams.ShiftInMs)));
                ++i;
                if (i == _testParams.BatchSize)
                {
                    i = 0;
                    Thread.Sleep(_testParams.BatchDelayInMs);
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // strictly speaking this may not be necessary - terminating the ActorSystem also works
            // but this call guarantees that the shutdown of the cluster is graceful regardless
            await CoordinatedShutdown.Get(ClusterSystem)
                .Run(CoordinatedShutdown.ClrExitReason.Instance);
        }
    }
}
