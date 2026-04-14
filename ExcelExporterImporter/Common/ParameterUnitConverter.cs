using Autodesk.Revit.DB;

namespace ExcelExporterImporter.Common
{
    public static class ParameterUnitConverter
    {
        /// <summary>
        ///     Converts the number to the correct unit for export
        /// </summary>
        /// <param name="param"></param>
        /// <param name="scheduleField"></param>
        /// <returns></returns>
        public static double AsProjectUnitTypeDouble(this Parameter param, ScheduleField scheduleField = null)
        {
            var imperialValue = param.AsDouble();
            var document = param.Element.Document;
            var dataTypeId = RevitUtilities.GetDefinitionDataTypeId(param.Definition);
            if (dataTypeId == null || dataTypeId.Empty())
            {
                return imperialValue;
            }

            var formatOptions = document.GetUnits().GetFormatOptions(dataTypeId);
            if (scheduleField != null)
            {
                var fieldFormatOptions = scheduleField.GetFormatOptions();
                if (!fieldFormatOptions.UseDefault)
                {
                    formatOptions = fieldFormatOptions;
                }
            }

            return UnitUtils.ConvertFromInternalUnits(imperialValue, formatOptions.GetUnitTypeId());
        }

        /// <summary>
        ///     Converts the number to the correct unit for import
        /// </summary>
        /// <param name="param"></param>
        /// <param name="valueToConvert"></param>
        /// <param name="scheduleField"></param>
        /// <returns></returns>
        public static double ToProjectUnitType(this Parameter param, double valueToConvert,
            ScheduleField scheduleField = null)
        {
            var document = param.Element.Document;
            var dataTypeId = RevitUtilities.GetDefinitionDataTypeId(param.Definition);
            if (dataTypeId == null || dataTypeId.Empty())
            {
                return valueToConvert;
            }

            var formatOptions = document.GetUnits().GetFormatOptions(dataTypeId);
            if (scheduleField != null)
            {
                var fieldFormatOptions = scheduleField.GetFormatOptions();
                if (!fieldFormatOptions.UseDefault)
                {
                    formatOptions = fieldFormatOptions;
                }
            }

            return UnitUtils.ConvertToInternalUnits(valueToConvert, formatOptions.GetUnitTypeId());
        }
    }
}