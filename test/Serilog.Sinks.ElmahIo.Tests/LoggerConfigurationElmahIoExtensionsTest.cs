using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Serilog.Core;
using System;
using System.Linq;
using System.Reflection;

namespace Serilog.Sinks.ElmahIo.Tests
{
    public class LoggerConfigurationElmahIoExtensionsTest
    {
        [Test]
        public void CanConfigureFromOptions()
        {
            var log = new LoggerConfiguration()
                .WriteTo.ElmahIo(new ElmahIoSinkOptions("apiKey", Guid.NewGuid()))
                .CreateLogger();

            AssertInstalledSink(log);
        }

        [Test]
        public void CanConfigureFromStringAndGuid()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var log = new LoggerConfiguration()
                .WriteTo.ElmahIo("apiKey", Guid.NewGuid().ToString())
                .CreateLogger();
#pragma warning restore CS0618 // Type or member is obsolete

            AssertInstalledSink(log);
        }

        [Test]
        public void CanConfigureFromAppsettings()
        {
            var configuration = new ConfigurationBuilder()
                .AddObject(new
                {
                    Serilog = new
                    {
                        Using = new[] { "Serilog.Sinks.ElmahIo" },
                        MinimumLevel = new
                        {
                            Default = "Information"
                        },
                        WriteTo = new[]
                        {
                            new
                            {
                                Name = "ElmahIo",
                                Args = new
                                {
                                    apiKey = "API_KEY",
                                    logId = Guid.NewGuid().ToString(),
                                }
                            }
                        }
                    }
                })
                .Build();

            var log = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            AssertInstalledSink(log);
        }

        private static void AssertInstalledSink(Logger log)
        {
            var aggregateSinkFieldInfo = log
                .GetType()
                .GetField("_sink", BindingFlags.Instance | BindingFlags.NonPublic);
            var aggregateSink = (ILogEventSink)aggregateSinkFieldInfo?.GetValue(log);
            var sinkEnumerableFieldInfo = aggregateSink?
                .GetType()
                .GetField("_sinks", BindingFlags.Instance | BindingFlags.NonPublic);
            var sinks = (ILogEventSink[])sinkEnumerableFieldInfo?.GetValue(aggregateSink);
            Assert.That(sinks.Count, Is.EqualTo(1));
        }
    }
}
