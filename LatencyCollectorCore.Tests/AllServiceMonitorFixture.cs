using System;
using System.Collections.Generic;
using System.IO;
using AppMetrics.Client;
using CIAPI.Rpc;
using CIAPI.Serialization;
using CIAPI.Streaming.Testing;
using LatencyCollectorCore.Monitors;
using NUnit.Framework;
using Salient.ReflectiveLoggingAdapter;
using Salient.ReliableHttpClient;
using Salient.ReliableHttpClient.Testing;

namespace LatencyCollectorCore.Tests
{
    [TestFixture]
    public class AllServiceMonitorFixture
    {
        static AllServiceMonitorFixture()
        {
            LogManager.CreateInnerLogger =
                (logName, logLevel, showLevel, showDateTime, showLogName, dateTimeFormat) =>
                new SimpleTraceAppender(logName, logLevel, showLevel, showDateTime, showLogName, dateTimeFormat);
        }
        
        [Test, Ignore("WIP: Should use playback so this can run without hitting the actual API")]
        public void ShouldExecuteWithoutExceptions()
        {
            var monitor = new AllServiceMonitor
                              {
                                  ServerUrl = "https://ciapi.cityindex.com/tradingapi",
                                  StreamingServerUrl = "https://push.cityindex.com",
                                  UserName = "DM603751",
                                  Password = "password",
                                  AllowTrading = false
                              };
            monitor.Tracker = new Tracker(monitor.LogEventUrl, "testTracker");
            
            var streamingFactory = new TestStreamingClientFactory();
            var requestFactory = new TestRequestFactory();

            monitor.ApiClient = new Client(new Uri(monitor.ServerUrl), new Uri(monitor.StreamingServerUrl), "TEST-APP-KEY", new Serializer(), requestFactory, streamingFactory);

            var serialized = File.ReadAllText("AllServiceMonitorRequests.txt");
            var requests = monitor.ApiClient.Serializer.DeserializeObject<List<RequestInfoBase>>(serialized);
            var finder = new TestWebRequestFinder { Reference = requests };

            requestFactory.PrepareResponse = testRequest =>
            {

                var match = finder.FindMatchExact(testRequest);

                if (match == null)
                {
                    throw new Exception("no matching request found");
                }

                finder.PopulateRequest(testRequest, match);

            };

            monitor.Execute();
        }

      
    }
}
