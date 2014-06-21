﻿using System;
using System.Collections.Generic;
using System.Linq;
using Abc.Zebus.Directory;
using Abc.Zebus.Routing;
using Abc.Zebus.Testing;
using Abc.Zebus.Testing.Measurements;
using NUnit.Framework;

namespace Abc.Zebus.Tests.Directory
{
    public partial class PeerDirectoryClientTests
    {
        [Test, Ignore("Performance test")]
        public void MeasureUpdatePerformance()
        {
            var subscriptions = new List<Subscription>();
            for (var typeIdIndex = 0; typeIdIndex < 20; ++typeIdIndex)
            {
                var typeId = new MessageTypeId("Abc.Foo.Events.FakeEvent" + typeIdIndex);
                for (var routingIndex = 0; routingIndex < 500; ++routingIndex)
                {
                    subscriptions.Add(new Subscription(typeId, new BindingKey(routingIndex.ToString())));
                }
            }

            var subscriptionsByTypeId = subscriptions.GroupBy(x => x.MessageTypeId).ToDictionary(x => x.Key, x => x.Select(s=>s.BindingKey).ToArray());

            _directory = new PeerDirectoryClient(_configurationMock.Object);
            _directory.Handle(new PeerStarted(_otherPeer.ToPeerDescriptor(false)));

            Console.WriteLine("Snapshot updates (add)");
            using (Measure.Throughput(subscriptions.Count))
            {
                for (var subscriptionCount = 1; subscriptionCount <= subscriptions.Count; ++subscriptionCount)
                {
                    _directory.Handle(new PeerSubscriptionsUpdated(_otherPeer.ToPeerDescriptor(false, subscriptions.Take(subscriptionCount))));
                }
            }
            Console.WriteLine("Snapshot updates (remove)");
            using (Measure.Throughput(subscriptions.Count))
            {
                for (var subscriptionCount = subscriptions.Count; subscriptionCount >= 1; --subscriptionCount)
                {
                    _directory.Handle(new PeerSubscriptionsUpdated(_otherPeer.ToPeerDescriptor(false, subscriptions.Take(subscriptionCount))));
                }
            }

            _directory = new PeerDirectoryClient(_configurationMock.Object);
            _directory.Handle(new PeerStarted(_otherPeer.ToPeerDescriptor(false)));

            Console.WriteLine("Snapshot updates per message type id (add)");
            using (Measure.Throughput(subscriptions.Count))
            {
                foreach (var subscriptionGroup in subscriptionsByTypeId)
                {
                    _directory.Handle(new PeerSubscriptionsForTypesUpdated(_otherPeer.Id, DateTime.UtcNow, subscriptionGroup.Key, subscriptionGroup.Value));
                }
            }
            Console.WriteLine("Snapshot updates per message type id (remove)");
            using (Measure.Throughput(subscriptions.Count))
            {
                foreach (var subscriptionGroup in subscriptionsByTypeId)
                {
                    _directory.Handle(new PeerSubscriptionsForTypesUpdated(_otherPeer.Id, DateTime.UtcNow, subscriptionGroup.Key));
                }
            }
        }

        [Test, Ignore("Performance test")]
        public void MeasureMemoryConsumption()
        {
            Console.WriteLine("Breakpoint here");

            for (var litePeerIndex = 0; litePeerIndex < 100; ++litePeerIndex)
            {
                var subscriptions = new List<Subscription>();
                for (var subscriptionIndex = 0; subscriptionIndex < 10; ++subscriptionIndex)
                {
                    subscriptions.Add(new Subscription(new MessageTypeId("Abc.Common.SharedEvent" + subscriptionIndex)));
                }

                for (var subscriptionIndex = 0; subscriptionIndex < 10; ++subscriptionIndex)
                {
                    subscriptions.Add(new Subscription(new MessageTypeId("Abc.Specific" + litePeerIndex + ".PrivateEvent" + subscriptionIndex)));
                }
                _directory.Handle(new PeerStarted(new PeerDescriptor(new PeerId("Abc.Testing.Peer" + litePeerIndex), "tcp://testing:11" + litePeerIndex, true, true, true, DateTime.UtcNow, subscriptions.ToArray())));
            }

            for (var fatPeerIndex = 0; fatPeerIndex < 30; ++fatPeerIndex)
            {
                var subscriptions = new List<Subscription>();
                for (var messageTypeIndex = 0; messageTypeIndex < 10; ++messageTypeIndex)
                {
                    var messageTypeId = new MessageTypeId("Abc.Common.SharedFatEvent" + messageTypeIndex);
                    for (var routingKeyIndex = 0; routingKeyIndex < 1000; ++routingKeyIndex)
                    {
                        subscriptions.Add(new Subscription(messageTypeId, new BindingKey(routingKeyIndex.ToString() + "00")));
                    }
                }
                _directory.Handle(new PeerStarted(new PeerDescriptor(new PeerId("Abc.Testing.FatPeer" + fatPeerIndex), "tcp://testing:22" + fatPeerIndex, true, true, true, DateTime.UtcNow, subscriptions.ToArray())));
            }

            Console.WriteLine("Breakpoint here");

            GC.Collect();

            Console.WriteLine("Breakpoint here");
        }
    }
}