﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Elmah.Io.Client;
using NSubstitute;
using NUnit.Framework;
using Serilog.Events;

namespace Serilog.Sinks.ElmahIo.Tests.Sinks.ElmahIo
{
    public class ElmahIoSinkTest
    {
        private readonly DateTimeOffset Now = DateTimeOffset.Now;
        private IList<CreateMessage> loggedMessages;
        private IElmahioAPI clientMock;

        [SetUp]
        public void SetUp()
        {
            loggedMessages = null;
            clientMock = Substitute.For<IElmahioAPI>();
            var messagesMock = Substitute.For<IMessagesClient>();
            clientMock.Messages.Returns(messagesMock);
            messagesMock
                .When(x => x.CreateBulkAndNotifyAsync(Arg.Any<Guid>(), Arg.Any<IList<CreateMessage>>()))
                .Do(x => loggedMessages = x.Arg<IList<CreateMessage>>());
        }

        [Test]
        public async Task CanEmitCustomFields()
        {
            // Arrange
            var sink = new ElmahIoSink(new ElmahIoSinkOptions(string.Empty, Guid.Empty), clientMock);
            var serverVariables = new Dictionary<ScalarValue, LogEventPropertyValue>
            {
                { new ScalarValue("serverVariableKey"), new ScalarValue("serverVariableValue") }
            };
            var cookies = new Dictionary<ScalarValue, LogEventPropertyValue>
            {
                { new ScalarValue("cookiesKey"), new ScalarValue("cookiesValue") }
            };
            var form = new Dictionary<ScalarValue, LogEventPropertyValue>
            {
                { new ScalarValue("formKey"), new ScalarValue("formValue") }
            };
            var queryString = new Dictionary<ScalarValue, LogEventPropertyValue>
            {
                { new ScalarValue("queryStringKey"), new ScalarValue("queryStringValue") }
            };

            // Act
            await sink.EmitBatchAsync(
            [
                new(
                    Now,
                    LogEventLevel.Error,
                    null,
                    new MessageTemplate("{type} {hostname} {application} {user} {source} {method} {version} {url} {statusCode}", []),
                    [
                        new("type", new ScalarValue("type")),
                        new("hostname", new ScalarValue("hostname")),
                        new("application", new ScalarValue("application")),
                        new("user", new ScalarValue("user")),
                        new("source", new ScalarValue("source")),
                        new("method", new ScalarValue("method")),
                        new("version", new ScalarValue("version")),
                        new("url", new ScalarValue("url")),
                        new("statusCode", new ScalarValue(400)),
                        new("category", new ScalarValue("category")),
                        new("correlationId", new ScalarValue("correlationId")),
                        new("servervariables", new DictionaryValue(serverVariables)),
                        new("cookies", new DictionaryValue(cookies)),
                        new("form", new DictionaryValue(form)),
                        new("querystring", new DictionaryValue(queryString)),
                    ]
                )
            ]);

            // Assert
            Assert.That(loggedMessages != null);
            Assert.That(loggedMessages.Count, Is.EqualTo(1));
            var loggedMessage = loggedMessages[0];
            Assert.That(loggedMessage.Type, Is.EqualTo("type"));
            Assert.That(loggedMessage.Hostname, Is.EqualTo("hostname"));
            Assert.That(loggedMessage.Application, Is.EqualTo("application"));
            Assert.That(loggedMessage.User, Is.EqualTo("user"));
            Assert.That(loggedMessage.Source, Is.EqualTo("source"));
            Assert.That(loggedMessage.Method, Is.EqualTo("method"));
            Assert.That(loggedMessage.Version, Is.EqualTo("version"));
            Assert.That(loggedMessage.Url, Is.EqualTo("url"));
            Assert.That(loggedMessage.StatusCode, Is.EqualTo(400));
            Assert.That(loggedMessage.CorrelationId, Is.EqualTo("correlationId"));
            Assert.That(loggedMessage.Category, Is.EqualTo("category"));
            Assert.That(loggedMessage.ServerVariables.Any(sv => sv.Key == "serverVariableKey" && sv.Value == "serverVariableValue"));
            Assert.That(loggedMessage.Cookies.Any(sv => sv.Key == "cookiesKey" && sv.Value == "cookiesValue"));
            Assert.That(loggedMessage.Form.Any(sv => sv.Key == "formKey" && sv.Value == "formValue"));
            Assert.That(loggedMessage.QueryString.Any(sv => sv.Key == "queryStringKey" && sv.Value == "queryStringValue"));
            Assert.That(!loggedMessage.Data.Any(d => d.Key == "servervariables"));
            Assert.That(!loggedMessage.Data.Any(d => d.Key == "cookies"));
            Assert.That(!loggedMessage.Data.Any(d => d.Key == "form"));
            Assert.That(!loggedMessage.Data.Any(d => d.Key == "querystring"));
        }

        [Test]
        public async Task CanEmit()
        {
            // Arrange
            var sink = new ElmahIoSink(new ElmahIoSinkOptions(string.Empty, Guid.Empty), clientMock);
            var exception = Exception();
            var principalMock = Substitute.For<IPrincipal>();
            var identityMock = Substitute.For<IIdentity>();
            identityMock.Name.Returns("User");
            principalMock.Identity.Returns(identityMock);
            Thread.CurrentPrincipal = principalMock;

            // Act
            await sink.EmitBatchAsync(
            [
                new(
                    Now,
                    LogEventLevel.Error,
                    exception,
                    new MessageTemplate("Simple test", []),
                    [
                        new("name", new ScalarValue("value"))
                    ]
                )
            ]);

            // Assert
            Assert.That(loggedMessages != null);
            Assert.That(loggedMessages.Count, Is.EqualTo(1));
            var loggedMessage = loggedMessages[0];
            Assert.That(loggedMessage.Severity, Is.EqualTo(Severity.Error.ToString()));
            Assert.That(loggedMessage.DateTime.HasValue);
            Assert.That(loggedMessage.DateTime.Value.DateTime, Is.EqualTo(Now.DateTime.ToUniversalTime()));
            Assert.That(loggedMessage.Detail, Is.EqualTo(exception.ToString()));
            Assert.That(loggedMessage.Data != null);
            Assert.That(loggedMessage.Data.Any(d => d.Key == "name"));
#if DOTNETCORE
            Assert.That(loggedMessage.Data.Any(d => d.Key == "X-ELMAHIO-FRAMEWORKDESCRIPTION"));
#endif
            Assert.That(loggedMessage.Data.Any(d => d.Key == "X-ELMAHIO-EXCEPTIONINSPECTOR"));
            Assert.That(loggedMessage.Type, Is.EqualTo(typeof(DivideByZeroException).FullName));
            Assert.That(loggedMessage.Hostname, Is.EqualTo(Environment.MachineName));
            Assert.That(loggedMessage.User, Is.EqualTo("User"));
        }

        [Test]
        public async Task CanEmitApplicationFromOptions()
        {
            // Arrange
            var sink = new ElmahIoSink(new ElmahIoSinkOptions(string.Empty, Guid.Empty) { Application = "MyApp" }, clientMock);

            // Act
            await sink.EmitBatchAsync(
            [
                new(Now, LogEventLevel.Information, null, new MessageTemplate("Hello World", []), [])
            ]);

            // Assert
            Assert.That(loggedMessages != null);
            Assert.That(loggedMessages.Count, Is.EqualTo(1));
            var loggedMessage = loggedMessages[0];
            Assert.That(loggedMessage.Application, Is.EqualTo("MyApp"));
        }

        [Test]
        public async Task CanEmitCategoryFromSourceContext()
        {
            // Arrange
            var sink = new ElmahIoSink(new ElmahIoSinkOptions(string.Empty, Guid.Empty), clientMock);

            // Act
            await sink.EmitBatchAsync(
            [
                new(
                    Now,
                    LogEventLevel.Error,
                    null,
                    new MessageTemplate("Test", []),
                    [
                        new("SourceContext", new ScalarValue("category")),
                    ]
                )
            ]);

            // Assert
            Assert.That(loggedMessages != null);
            Assert.That(loggedMessages.Count, Is.EqualTo(1));
            var loggedMessage = loggedMessages[0];
            Assert.That(loggedMessage.Category, Is.EqualTo("category"));
        }

        [Test]
        public async Task CanFilterBatchFromOptions()
        {
            // Arrange
            var messages = 0;
            var options = new ElmahIoSinkOptions("API_KEY", Guid.NewGuid())
            {
                OnFilter = msg => true,
                OnMessage = msg => messages++
            };
            var sink = new ElmahIoSink(options);

            // Act
            await sink.EmitBatchAsync(
            [
                new(Now, LogEventLevel.Error, null, new MessageTemplate("Test1", []), [])
            ]);

            // Assert
            Assert.That(messages, Is.EqualTo(0));

        }

        private static Exception Exception()
        {
            return new Exception("error", new DivideByZeroException());
        }
    }
}