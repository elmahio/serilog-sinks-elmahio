using System;
using System.Collections.Generic;
using System.Linq;
using Elmah.Io.Client;
using Moq;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.ElmahIO.Test.Sinks.ElmahIO
{
    public class ElmahIOSinkTest
    {
        [Test]
        public void CanEmit()
        {
            // Arrange
            var loggerMock = new Mock<Elmah.Io.Client.ILogger>();
            var sink = new ElmahIOSink(null, loggerMock.Object);
            Message loggedMessage = null;
            loggerMock
                .Setup(x => x.Log(It.IsAny<Message>()))
                .Callback<Message>(msg =>
                {
                    loggedMessage = msg;
                });
            var now = DateTimeOffset.Now;
            var applicationException = new ApplicationException();

            // Act
            sink.Emit(
                new LogEvent(
                    now,
                    LogEventLevel.Error,
                    applicationException,
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
            Assert.That(loggedMessage.Detail, Is.EqualTo(applicationException.ToString()));
            Assert.That(loggedMessage.Data != null);
            Assert.That(loggedMessage.Data.Count, Is.EqualTo(1));
            Assert.That(loggedMessage.Data.First().Key, Is.EqualTo("name"));
        }
    }
}