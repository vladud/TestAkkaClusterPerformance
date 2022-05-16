﻿using Akka.Cluster.Sharding;

namespace TestAkkaClusterPerformance
{
    public class ShardEnvelope
    {
        public readonly string EntityId;
        public readonly object Payload;

        public ShardEnvelope(string entityId, object payload)
        {
            EntityId = entityId;
            Payload = payload;
        }
    }

    public sealed class MessageExtractor : HashCodeMessageExtractor
    {
        public MessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
        {
        }

        public override string EntityId(object message)
        {
            switch (message)
            {
                case ShardRegion.StartEntity start: return start.EntityId;
                case ShardEnvelope e: return e.EntityId.ToString();
            }

            return null;
        }

        public override object EntityMessage(object message)
        {
            switch (message)
            {
                case ShardEnvelope e: return e.Payload;
                default:
                    return message;
            }
        }
    }
}
