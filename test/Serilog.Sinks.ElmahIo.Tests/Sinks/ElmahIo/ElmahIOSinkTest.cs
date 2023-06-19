using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Elmah.Io.Client;
using NSubstitute;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.ElmahIo.Tests
{
    public class ElmahIoSinkTest
    {
        private DateTimeOffset Now = DateTimeOffset.Now;
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
            var serverVariables = new Dictionary<ScalarValue, LogEventPropertyValue>();
            serverVariables.Add(new ScalarValue("serverVariableKey"), new ScalarValue("serverVariableValue"));
            var cookies = new Dictionary<ScalarValue, LogEventPropertyValue>();
            cookies.Add(new ScalarValue("cookiesKey"), new ScalarValue("cookiesValue"));
            var form = new Dictionary<ScalarValue, LogEventPropertyValue>();
            form.Add(new ScalarValue("formKey"), new ScalarValue("formValue"));
            var queryString = new Dictionary<ScalarValue, LogEventPropertyValue>();
            queryString.Add(new ScalarValue("queryStringKey"), new ScalarValue("queryStringValue"));

            // Act
            await sink.EmitBatchAsync(new List<LogEvent>
            {
                new LogEvent(
                    Now,
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
                        new LogEventProperty("category", new ScalarValue("category")),
                        new LogEventProperty("correlationId", new ScalarValue("correlationId")),
                        new LogEventProperty("serverVariables", new DictionaryValue(serverVariables)),
                        new LogEventProperty("cookies", new DictionaryValue(cookies)),
                        new LogEventProperty("form", new DictionaryValue(form)),
                        new LogEventProperty("queryString", new DictionaryValue(queryString)),
                    }
                )
            });

            // Assert
            Assert.That(loggedMessages != null);
            Assert.That(loggedMessages.Count, Is.EqualTo(1));
            var loggedMessage = loggedMessages.First();
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
            await sink.EmitBatchAsync(new List<LogEvent>
            {
                new LogEvent(
                    Now,
                    LogEventLevel.Error,
                    exception,
                    new MessageTemplate("Simple test", new List<MessageTemplateToken>()), new List<LogEventProperty>
                    {
                        new LogEventProperty("name", new ScalarValue("value"))
                    }
                )
            });

            // Assert
            Assert.That(loggedMessages != null);
            Assert.That(loggedMessages.Count, Is.EqualTo(1));
            var loggedMessage = loggedMessages.First();
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
#if !DOTNETCORE
            Assert.That(loggedMessage.User, Is.EqualTo("User"));
#endif
        }

        [Test]
        public async Task CanEmitApplicationFromOptions()
        {
            // Arrange
            var sink = new ElmahIoSink(new ElmahIoSinkOptions(string.Empty, Guid.Empty) { Application = "MyApp" }, clientMock);

            // Act
            await sink.EmitBatchAsync(new List<LogEvent>
            {
                new LogEvent(Now, LogEventLevel.Information, null, new MessageTemplate("Hello World", new List<MessageTemplateToken>()), new List<LogEventProperty>())
            });

            // Assert
            Assert.That(loggedMessages != null);
            Assert.That(loggedMessages.Count, Is.EqualTo(1));
            var loggedMessage = loggedMessages.First();
            Assert.That(loggedMessage.Application, Is.EqualTo("MyApp"));
        }

        [Test]
        public async Task CanEmitCategoryFromSourceContext()
        {
            // Arrange
            var sink = new ElmahIoSink(new ElmahIoSinkOptions(string.Empty, Guid.Empty), clientMock);

            // Act
            await sink.EmitBatchAsync(new List<LogEvent>
            {
                new LogEvent(
                    Now,
                    LogEventLevel.Error,
                    null,
                    new MessageTemplate("Test", new List<MessageTemplateToken>()), new List<LogEventProperty>
                    {
                        new LogEventProperty("SourceContext", new ScalarValue("category")),
                    }
                )
            });

            // Assert
            Assert.That(loggedMessages != null);
            Assert.That(loggedMessages.Count, Is.EqualTo(1));
            var loggedMessage = loggedMessages.First();
            Assert.That(loggedMessage.Category, Is.EqualTo("category"));
        }

        private Exception Exception()
        {
            return new Exception("error", new DivideByZeroException());
        }
    }
}