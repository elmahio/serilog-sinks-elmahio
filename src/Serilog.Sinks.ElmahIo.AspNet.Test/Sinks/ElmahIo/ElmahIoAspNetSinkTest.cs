using System;
using System.Collections.Generic;
using System.Linq;
using Elmah.Io.Client;
using Moq;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.ElmahIo.AspNet.Test.Sinks.ElmahIo
{
    public class ElmahIoAspNetSinkTest
    {
        [Test]
        public void CanEmit()
        {
            // Arrange
            var loggerMock = new Mock<Elmah.Io.Client.ILogger>();
            var sink = new ElmahIoAspNetSink(null, loggerMock.Object);
            Message loggedMessage = null;
            loggerMock
                .Setup(x => x.Log(It.IsAny<Message>()))
                .Callback<Message>(msg =>
                {
                    loggedMessage = msg;
                });
            var now = DateTimeOffset.Now;
            var exception = Exception();

            // Act
            sink.Emit(
                new LogEvent(
                    now,
                    LogEventLevel.Error,
                    exception,
                    new MessageTemplate("Simple test", new List<MessageTemplateToken>()), new List<LogEventProperty>
                    {
                        new LogEventProperty("name", new ScalarValue("value"))
                    }
                )
            );

            // Assert
            Assert.That(loggedMessage != null);
            Assert.That(loggedMessage.Severity, Is.EqualTo(Severity.Error));
            Assert.That(loggedMessage.DateTime, Is.EqualTo(now.DateTime.ToUniversalTime()));
            Assert.That(loggedMessage.Detail, Is.EqualTo(exception.ToString()));
            Assert.That(loggedMessage.Data != null);
            Assert.That(loggedMessage.Data.Count, Is.EqualTo(1));
            Assert.That(loggedMessage.Data.First().Key, Is.EqualTo("name"));
            Assert.That(loggedMessage.Type, Is.EqualTo(typeof(DivideByZeroException).FullName));
            Assert.That(loggedMessage.Hostname, Is.EqualTo(Environment.MachineName));
        }

        private static ApplicationException Exception()
        {
            return new ApplicationException("error", new DivideByZeroException());
        }
    }
}