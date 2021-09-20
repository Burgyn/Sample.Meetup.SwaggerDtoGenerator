using System.Globalization;

namespace Sample.SwaggerDtoGenerator.Demo
{
    public static class StringExtensions
    {
        public static string ToTitleCase(this string str)
            => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
    }
}
