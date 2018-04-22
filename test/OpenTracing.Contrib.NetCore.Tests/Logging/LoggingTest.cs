using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Mock;
using Xunit;

namespace OpenTracing.Contrib.NetCore.Tests.Logging
{
    public class LoggingTest
    {
        private readonly MockTracer _tracer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public LoggingTest()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddOpenTracingCoreServices(ot =>
                {
                    ot.AddLoggerProvider();
                    ot.Services.AddSingleton<ITracer, MockTracer>();
                    ot.Services.AddSingleton<IGlobalTracerAccessor>(sp =>
                    {
                        var globalTracerAccessor = Substitute.For<IGlobalTracerAccessor>();
                        globalTracerAccessor.GetGlobalTracer().Returns(sp.GetRequiredService<ITracer>());
                        return globalTracerAccessor;
                    });
                })
                .BuildServiceProvider();

            _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = _loggerFactory.CreateLogger("FooLogger");

            _tracer = (MockTracer)serviceProvider.GetRequiredService<ITracer>();
        }

        private IScope StartScope(string operationName = "FooOperation")
        {
            return _tracer.BuildSpan(operationName)
                .StartActive(finishSpanOnDispose: true);
        }

        private MockSpan.LogEntry Log(Action actionUnderScope)
        {
            using (StartScope())
            {
                actionUnderScope.Invoke();
            }

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            var logEntries = span.LogEntries;
            Assert.Single(logEntries);

            return logEntries[0];
        }

        [Fact]
        public void Logger_does_not_create_span_if_no_ActiveSpan()
        {
            _logger.LogInformation("Hello World");

            Assert.Empty(_tracer.FinishedSpans());
        }

        [Fact]
        public void Logger_adds_Log_to_ActiveSpan()
        {
            using (StartScope())
            {
                _logger.LogInformation("Hello World");
            }

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            var logEntries = span.LogEntries;
            Assert.Single(logEntries);
        }

        [Fact]
        public void Logger_adds_multiple_Log_to_ActiveSpan()
        {
            using (StartScope())
            {
                _logger.LogInformation("Hello World");
                _logger.LogWarning("Some warning");
            }

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            var logEntries = span.LogEntries;
            Assert.Equal(2, logEntries.Count);
        }

        [Fact]
        public void Correct_fields_for_LogInformation_with_null_message()
        {
            // TODO Is it useful to create log entries with a "[null]" message?

            var logEntry = Log(() => _logger.LogInformation(null));

            Assert.Equal(4, logEntry.Fields.Count);
            Assert.Equal("FooLogger", logEntry.Fields["component"]);
            Assert.Equal("Information", logEntry.Fields["level"]);
            Assert.Equal("[null]", logEntry.Fields[LogFields.Message]);
            Assert.Equal("[null]", logEntry.Fields[LogFields.Event]);
        }

        [Fact]
        public void Correct_fields_for_LogInformation_with_message()
        {
            // TODO is it ok to emit "Hello World" twice?

            var logEntry = Log(() => _logger.LogInformation("Hello World"));

            Assert.Equal(4, logEntry.Fields.Count);
            Assert.Equal("FooLogger", logEntry.Fields["component"]);
            Assert.Equal("Information", logEntry.Fields["level"]);
            Assert.Equal("Hello World", logEntry.Fields[LogFields.Message]);
            Assert.Equal("Hello World", logEntry.Fields[LogFields.Event]);
        }

        [Fact]
        public void Args_are_NOT_propagated_if_they_are_not_used_in_messageTemplate()
        {
            // This currently is by design: https://github.com/aspnet/Logging/issues/533

            int arg1 = 4;
            string arg2 = "bar";
            var logEntry = Log(() => _logger.LogInformation("Hello World", arg1, arg2));

            Assert.Equal(4, logEntry.Fields.Count);
            Assert.Equal("FooLogger", logEntry.Fields["component"]);
            Assert.Equal("Information", logEntry.Fields["level"]);
            Assert.Equal("Hello World", logEntry.Fields[LogFields.Message]);
            Assert.Equal("Hello World", logEntry.Fields[LogFields.Event]);
        }

        [Fact]
        public void Correct_fields_for_LogInformation_with_messageTemplate_without_args()
        {
            var logEntry = Log(() => _logger.LogInformation("Hello {Name}"));

            Assert.Equal(4, logEntry.Fields.Count);
            Assert.Equal("FooLogger", logEntry.Fields["component"]);
            Assert.Equal("Information", logEntry.Fields["level"]);
            Assert.Equal("Hello {Name}", logEntry.Fields[LogFields.Message]);
            Assert.Equal("Hello {Name}", logEntry.Fields[LogFields.Event]);
        }

        [Fact]
        public void Correct_fields_for_LogInformation_with_messageTemplate_and_not_enough_args()
        {
            // TODO This throws in Microsoft.Extensions.Logging when generating the "Message" and we currently just ignore it.

            string arg1 = "Max";
            var logEntry = Log(() => _logger.LogInformation("Hello {Arg1} {Arg2}", arg1));

            Assert.Equal(4, logEntry.Fields.Count);
            Assert.Equal("FooLogger", logEntry.Fields["component"]);
            Assert.Equal("Information", logEntry.Fields["level"]);
            Assert.Equal("log", logEntry.Fields[LogFields.Event]);
            Assert.Equal("Max", logEntry.Fields["Arg1"]);
        }

        [Fact]
        public void Correct_fields_for_LogInformation_with_messageTemplate_with_one_arg()
        {
            string name = "Max";
            var logEntry = Log(() => _logger.LogInformation("Hello {Name}", name));

            Assert.Equal(5, logEntry.Fields.Count);
            Assert.Equal("FooLogger", logEntry.Fields["component"]);
            Assert.Equal("Information", logEntry.Fields["level"]);
            Assert.Equal("Hello Max", logEntry.Fields[LogFields.Message]);
            Assert.Equal("Hello {Name}", logEntry.Fields[LogFields.Event]);
            Assert.Equal("Max", logEntry.Fields["Name"]);
        }

        [Fact]
        public void Correct_fields_for_LogInformation_with_messageTemplate_with_two_arg()
        {
            string arg1 = "Max";
            int arg2 = 3;
            var logEntry = Log(() => _logger.LogInformation("Hello {Arg1}, {Arg2}", arg1, arg2));

            Assert.Equal(6, logEntry.Fields.Count);
            Assert.Equal("FooLogger", logEntry.Fields["component"]);
            Assert.Equal("Information", logEntry.Fields["level"]);
            Assert.Equal("Hello Max, 3", logEntry.Fields[LogFields.Message]);
            Assert.Equal("Hello {Arg1}, {Arg2}", logEntry.Fields[LogFields.Event]);
            Assert.Equal("Max", logEntry.Fields["Arg1"]);
            Assert.Equal(3, logEntry.Fields["Arg2"]);
        }

        [Fact]
        public void Arg_overwrites_builtin_fields()
        {
            // TODO Is this behavior ok?

            string level = "Test";
            var logEntry = Log(() => _logger.LogInformation("Hello {level}", level));

            Assert.Equal(4, logEntry.Fields.Count);
            Assert.Equal("FooLogger", logEntry.Fields["component"]);
            Assert.Equal("Test", logEntry.Fields["level"]);
            Assert.Equal("Hello Test", logEntry.Fields[LogFields.Message]);
            Assert.Equal("Hello {level}", logEntry.Fields[LogFields.Event]);
        }

        [Fact]
        public void Correct_fields_for_LogInformation_with_eventId()
        {
            var logEntry = Log(() => _logger.LogInformation(new EventId(1), "Hello World"));

            Assert.Equal(5, logEntry.Fields.Count);
            Assert.Equal("FooLogger", logEntry.Fields["component"]);
            Assert.Equal("Information", logEntry.Fields["level"]);
            Assert.Equal("Hello World", logEntry.Fields[LogFields.Message]);
            Assert.Equal("Hello World", logEntry.Fields[LogFields.Event]);
            Assert.Equal(1, logEntry.Fields["eventId"]);
        }

        [Fact]
        public void Correct_fields_for_LogInformation_with_exception()
        {
            var exception = new InvalidOperationException("Something went wrong");
            var logEntry = Log(() => _logger.LogInformation(exception, "Hello World"));

            Assert.Equal(6, logEntry.Fields.Count);
            Assert.Equal("FooLogger", logEntry.Fields["component"]);
            Assert.Equal("Information", logEntry.Fields["level"]);
            Assert.Equal("Hello World", logEntry.Fields[LogFields.Message]);
            Assert.Equal("Hello World", logEntry.Fields[LogFields.Event]);
            Assert.Equal("System.InvalidOperationException", logEntry.Fields[LogFields.ErrorKind]); // TODO use FullName or Name?
            Assert.Same(exception, logEntry.Fields[LogFields.ErrorObject]);
        }

        [Fact]
        public void Correct_fields_for_LogInformation_with_exception_and_args()
        {
            var arg1 = "Max";
            var arg2 = 3;
            var exception = new InvalidOperationException("Something went wrong");
            var logEntry = Log(() => _logger.LogInformation(exception, "Hello World {Arg1} {Arg2}", arg1, arg2));

            Assert.Equal(8, logEntry.Fields.Count);
            Assert.Equal("FooLogger", logEntry.Fields["component"]);
            Assert.Equal("Information", logEntry.Fields["level"]);
            Assert.Equal("Hello World Max 3", logEntry.Fields[LogFields.Message]);
            Assert.Equal("Hello World {Arg1} {Arg2}", logEntry.Fields[LogFields.Event]);
            Assert.Equal("Max", logEntry.Fields["Arg1"]);
            Assert.Equal(3, logEntry.Fields["Arg2"]);
            Assert.Equal("System.InvalidOperationException", logEntry.Fields[LogFields.ErrorKind]); // TODO use FullName or Name?
            Assert.Same(exception, logEntry.Fields[LogFields.ErrorObject]);
        }
    }
}
