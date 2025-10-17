namespace FormBuilderAPI.Helpers
{
    public static class FieldTypeHelper
    {
        public static bool IsChoice(string? type)
        {
            var t = (type ?? "").Trim().ToLowerInvariant();
            return t is "radio" or "dropdown" or "checkbox" or "multiselect" or "mcq" or "multiple";
        }
    }
}