using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using Moq;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.ElmahIo.Tests
{
    public class ElmahIoSinkTest
    {
        [Test]
        public void CanEmitCustomFields()
        {
            // Arrange
            var clientMock = new Mock<IElmahioAPI>();
            var messagesMock = new Mock<IMessages>();
            clientMock.Setup(x => x.Messages).Returns(messagesMock.Object);
            var sink = new ElmahIoSink(new ElmahIoSinkOptions(), clientMock.Object);
            CreateMessage loggedMessage = null;
            messagesMock
                .Setup(x => x.CreateAndNotify(It.IsAny<Guid>(), It.IsAny<CreateMessage>()))
                .Callback<Guid, CreateMessage>((logId, msg) =>
                {
                    loggedMessage = msg;
                });
            var now = DateTimeOffset.Now;

            // Act
            sink.Emit(
                new LogEvent(
                    now,
                    LogEventLevel.Error,
                    null,
                    new MessageTemplate("{type} {hostname} {application} {user} {source} {method} {version} {url} {statusCode}", new List<MessageTemplateToken>()), new List<LogEventProperty>
                    {
                        new LogEventProperty("type", new ScalarValue("type")),
                        new LogEventProperty("hostname", new ScalarValue("hostname")),
                        new LogEventProperty("application", new ScalarValue("application")),
                        new LogEventProperty("user", new ScalarValue("user")),
                        new LogEventProperty("source", new ScalarValue("source")),
                        new LogEventProperty("method", new ScalarValue("method")),
                        new LogEventProperty("version", new ScalarValue("version")),
                        new LogEventProperty("url", new ScalarValue("url")),
                        new LogEventProperty("statusCode", new ScalarValue(400)),
                    }
                )
            );

            // Assert
            sink.Dispose();
            Assert.That(loggedMessage != null);
            Assert.That(loggedMessage.Type, Is.EqualTo("type"));
            Assert.That(loggedMessage.Hostname, Is.EqualTo("hostname"));
            Assert.That(loggedMessage.Application, Is.EqualTo("application"));
            Assert.That(loggedMessage.User, Is.EqualTo("user"));
            Assert.That(loggedMessage.Source, Is.EqualTo("source"));
            Assert.That(loggedMessage.Method, Is.EqualTo("method"));
            Assert.That(loggedMessage.Version, Is.EqualTo("version"));
            Assert.That(loggedMessage.Url, Is.EqualTo("url"));
            Assert.That(loggedMessage.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public void CanEmit()
        {
            // Arrange
            var clientMock = new Mock<IElmahioAPI>();
            var messagesMock = new Mock<IMessages>();
            clientMock.Setup(x => x.Messages).Returns(messagesMock.Object);
            var sink = new ElmahIoSink(new ElmahIoSinkOptions(), clientMock.Object);
            CreateMessage loggedMessage = null;
            messagesMock
                .Setup(x => x.CreateAndNotify(It.IsAny<Guid>(), It.IsAny<CreateMessage>()))
                .Callback<Guid, CreateMessage>((logId, msg) =>
                {
                    loggedMessage = msg;
                });
            var now = DateTimeOffset.Now;
            var exception = Exception();
#if !DOTNETCORE
            var principalMock = new Mock<IPrincipal>();
            var identityMock = new Mock<IIdentity>();
            identityMock.Setup(x => x.Name).Returns("User");
            principalMock.Setup(x => x.Identity).Returns(identityMock.Object);
            Thread.CurrentPrincipal = principalMock.Object;
#endif

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
            sink.Dispose();
            Assert.That(loggedMessage != null);
            Assert.That(loggedMessage.Severity, Is.EqualTo(Severity.Error.ToString()));
            Assert.That(loggedMessage.DateTime, Is.EqualTo(now.DateTime.ToUniversalTime()));
            Assert.That(loggedMessage.Detail, Is.EqualTo(exception.ToString()));
            Assert.That(loggedMessage.Data != null);
            Assert.That(loggedMessage.Data.Count, Is.EqualTo(1));
            Assert.That(loggedMessage.Data.First().Key, Is.EqualTo("name"));
            Assert.That(loggedMessage.Type, Is.EqualTo(typeof(DivideByZeroException).FullName));
            Assert.That(loggedMessage.Hostname, Is.EqualTo(Environment.MachineName));
#if !DOTNETCORE
            Assert.That(loggedMessage.User, Is.EqualTo("User"));
#endif
        }

        private Exception Exception()
        {
            return new Exception("error", new DivideByZeroException());
        }
    }
}