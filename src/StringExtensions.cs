using System.Globalization;
using System.Linq;

namespace Sample.Meetup.SwaggerDtoGenerator
{
    public static class StringExtensions
    {
        public static string ToTitleCase(this string str)
            => $"{char.ToUpper(str.First())}{str.Substring(1, str.Length - 1)}";
    }
}
