using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ExcelExporterImporter.Support
{
    public static class LogMessageFormatter
    {
        public static string Format(string eventName, IEnumerable<KeyValuePair<string, object>> properties = null)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentException("eventName must be provided.", "eventName");

            var parts = new List<string> { FormatPair("event", eventName) };
            if (properties != null)
            {
                parts.AddRange(properties
                    .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null)
                    .Select(pair => FormatPair(pair.Key, pair.Value)));
            }

            return string.Join(" ", parts);
        }

        private static string FormatPair(string key, object value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}=\"{1}\"", key, Escape(ConvertToString(value)));
        }

        private static string ConvertToString(object value)
        {
            if (value == null)
                return string.Empty;

            var dateTimeOffset = value as DateTimeOffset?;
            if (dateTimeOffset.HasValue)
                return dateTimeOffset.Value.ToString("o", CultureInfo.InvariantCulture);

            if (value is DateTime)
                return ((DateTime)value).ToString("o", CultureInfo.InvariantCulture);

            if (value is bool)
                return ((bool)value) ? "true" : "false";

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string Escape(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }
    }
}