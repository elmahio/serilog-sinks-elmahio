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

namespace Serilog.Sinks.ElmahIo.Test
{
    public class ElmahIoSinkTest
    {
        [Test]
        public void CanEmit()
        {
            // Arrange
            var clientMock = new Mock<IElmahioAPI>();
            var messagesMock = new Mock<IMessages>();
            clientMock.Setup(x => x.Messages).Returns(messagesMock.Object);
            var sink = new ElmahIoSink(null, clientMock.Object);
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