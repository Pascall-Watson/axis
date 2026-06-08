using System.Collections.Generic;
using ExcelExporterImporter.Interop;
using Xunit;

namespace ExcelExporterImporter.Tests
{
    public class OperationResultTests
    {
        [Fact]
        public void CancelledResult_UsesWarningsInsteadOfErrors()
        {
            var result = OperationResult.Cancelled;

            Assert.False(result.Success);
            Assert.True(result.IsCancelled);
            Assert.Empty(result.Errors);
            Assert.Single(result.Warnings);
            Assert.Equal("Operation was cancelled.", result.Warnings[0]);
            Assert.NotNull(result.Summary);
            Assert.Equal(0, result.Summary.FailedCount);
        }

        [Fact]
        public void OperationResult_PreservesSupportMetadata()
        {
            var result = new OperationResult(
                false,
                new List<string> { "Backend failed." },
                new List<string> { "Retry with the fallback." },
                new OperationSummary(4, 2, 1, 1),
                false,
                "import-workbook",
                "abc123",
                @"C:\Logs\backend.log");

            Assert.Equal("import-workbook", result.OperationName);
            Assert.Equal("abc123", result.OperationId);
            Assert.Equal(@"C:\Logs\backend.log", result.LogFilePath);
            Assert.Equal(4, result.Summary.RequestedCount);
            Assert.Equal(1, result.Summary.SkippedCount);
        }
    }
}