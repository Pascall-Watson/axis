using System;
using System.Collections.Generic;
using System.IO;
using ExcelExporterImporter.Interop;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace ExcelExporterImporter.Support
{
    public static class SupportLog
    {
        private const string BackendLogFileName = "backend.log";
        private static readonly object SyncRoot = new object();
        private static bool isConfigured;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SupportLog));

        public static string LogDirectoryPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Pascall-Watson",
                    "Axis");
            }
        }

        public static string LogFilePath
        {
            get { return Path.Combine(LogDirectoryPath, BackendLogFileName); }
        }

        public static void EnsureConfigured()
        {
            if (isConfigured)
                return;

            lock (SyncRoot)
            {
                if (isConfigured)
                    return;

                Directory.CreateDirectory(LogDirectoryPath);

                var hierarchy = LogManager.GetRepository() as Hierarchy;
                if (hierarchy != null)
                {
                    hierarchy.Root.RemoveAllAppenders();

                    var layout = new PatternLayout("%date{yyyy-MM-ddTHH:mm:ss.fffK} level=%level logger=%logger message=%message%newline%exception");
                    layout.ActivateOptions();

                    var appender = new RollingFileAppender
                    {
                        Name = "AxisSupportLog",
                        AppendToFile = true,
                        File = LogFilePath,
                        StaticLogFileName = true,
                        RollingStyle = RollingFileAppender.RollingMode.Size,
                        MaximumFileSize = "5MB",
                        MaxSizeRollBackups = 10,
                        LockingModel = new FileAppender.MinimalLock(),
                        Layout = layout,
                    };

                    appender.ActivateOptions();
                    hierarchy.Root.AddAppender(appender);
                    hierarchy.Root.Level = Level.Info;
                    hierarchy.Configured = true;
                }

                isConfigured = true;
            }
        }

        public static OperationLogContext StartOperation(
            string operationName,
            string source,
            string documentTitle,
            string targetPath,
            int requestedCount)
        {
            EnsureConfigured();

            var context = new OperationLogContext(
                operationName,
                source,
                Guid.NewGuid().ToString("N"),
                documentTitle,
                targetPath,
                requestedCount,
                LogFilePath);

            Info(
                "operation-start",
                new Dictionary<string, object>
                {
                    { "operation", context.OperationName },
                    { "operationId", context.OperationId },
                    { "source", context.Source },
                    { "document", context.DocumentTitle },
                    { "targetPath", context.TargetPath },
                    { "requestedCount", context.RequestedCount },
                });

            return context;
        }

        public static OperationResult CompleteOperation(OperationLogContext context, OperationResult result)
        {
            EnsureConfigured();

            var resolved = (result ?? new OperationResult(false, new List<string> { "Operation did not return a result." }, null))
                .WithSupportContext(context.OperationName, context.OperationId, context.LogFilePath);

            var summary = resolved.Summary;
            var level = resolved.Success ? "operation-success" : resolved.IsCancelled ? "operation-cancelled" : "operation-failed";

            Info(
                level,
                new Dictionary<string, object>
                {
                    { "operation", context.OperationName },
                    { "operationId", context.OperationId },
                    { "requestedCount", summary != null ? summary.RequestedCount : 0 },
                    { "succeededCount", summary != null ? summary.SucceededCount : 0 },
                    { "failedCount", summary != null ? summary.FailedCount : 0 },
                    { "skippedCount", summary != null ? summary.SkippedCount : 0 },
                    { "warningCount", resolved.Warnings.Count },
                    { "errorCount", resolved.Errors.Count },
                    { "isCancelled", resolved.IsCancelled },
                    { "success", resolved.Success },
                });

            return resolved;
        }

        public static OperationResult FailOperation(OperationLogContext context, string userMessage, Exception exception)
        {
            EnsureConfigured();

            Error(
                "operation-exception",
                exception,
                new Dictionary<string, object>
                {
                    { "operation", context.OperationName },
                    { "operationId", context.OperationId },
                    { "source", context.Source },
                    { "document", context.DocumentTitle },
                    { "targetPath", context.TargetPath },
                    { "requestedCount", context.RequestedCount },
                });

            return new OperationResult(
                false,
                new List<string>
                {
                    userMessage,
                    exception != null ? exception.Message : "No exception details were provided."
                },
                null,
                new OperationSummary(context.RequestedCount, 0, 1, 0),
                false,
                context.OperationName,
                context.OperationId,
                context.LogFilePath);
        }

        public static void Info(string eventName, IDictionary<string, object> properties)
        {
            EnsureConfigured();
            Logger.Info(LogMessageFormatter.Format(eventName, properties));
        }

        public static void Warn(string eventName, IDictionary<string, object> properties)
        {
            EnsureConfigured();
            Logger.Warn(LogMessageFormatter.Format(eventName, properties));
        }

        public static void Error(string eventName, Exception exception, IDictionary<string, object> properties)
        {
            EnsureConfigured();
            Logger.Error(LogMessageFormatter.Format(eventName, properties), exception);
        }
    }

    public sealed class OperationLogContext
    {
        public OperationLogContext(
            string operationName,
            string source,
            string operationId,
            string documentTitle,
            string targetPath,
            int requestedCount,
            string logFilePath)
        {
            OperationName = operationName;
            Source = source;
            OperationId = operationId;
            DocumentTitle = documentTitle;
            TargetPath = targetPath;
            RequestedCount = requestedCount;
            LogFilePath = logFilePath;
        }

        public string OperationName { get; }

        public string Source { get; }

        public string OperationId { get; }

        public string DocumentTitle { get; }

        public string TargetPath { get; }

        public int RequestedCount { get; }

        public string LogFilePath { get; }
    }
}