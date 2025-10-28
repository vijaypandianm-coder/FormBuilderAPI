// Helpers/ValidationHelper.cs
using System;
using System.Globalization;

namespace FormBuilderAPI.Helpers
{
    public static class ValidationHelper
    {
        public const int ShortTextMax = 100;
        public const int LongTextMax = 500;
        public const string DateFormat = "dd/MM/yyyy";

        public static bool IsInteger(string? value)
            => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

        public static bool TryParseDateDdMmYyyy(string? value, out DateTime date)
            => DateTime.TryParseExact(value ?? "", DateFormat, CultureInfo.InvariantCulture,
                                      DateTimeStyles.None, out date);
    }
}