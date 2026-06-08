using System.Collections.Generic;
using ExcelExporterImporter.Support;
using Xunit;

namespace ExcelExporterImporter.Tests
{
    public class LogMessageFormatterTests
    {
        [Fact]
        public void Format_IncludesEventAndEscapedValues()
        {
            var formatted = LogMessageFormatter.Format(
                "operation-start",
                new[]
                {
                    new KeyValuePair<string, object>("document", "Level 1 \"Pilot\""),
                    new KeyValuePair<string, object>("requestedCount", 3),
                });

            Assert.Equal(
                "event=\"operation-start\" document=\"Level 1 \\\"Pilot\\\"\" requestedCount=\"3\"",
                formatted);
        }

        [Fact]
        public void Format_StripsNewLinesFromValues()
        {
            var formatted = LogMessageFormatter.Format(
                "operation-error",
                new[]
                {
                    new KeyValuePair<string, object>("message", "Line 1\r\nLine 2"),
                });

            Assert.Equal("event=\"operation-error\" message=\"Line 1  Line 2\"", formatted);
        }
    }
}