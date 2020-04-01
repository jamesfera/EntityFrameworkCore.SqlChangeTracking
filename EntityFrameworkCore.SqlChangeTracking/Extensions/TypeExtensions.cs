using System;
using System.Linq;

namespace EntityFrameworkCore.SqlChangeTracking
{
    internal static class TypeExtensions
    {
        public static string PrettyName(this Type type)
        {
            if (type.IsGenericType)
                return $"{type.FullName.Substring(0, type.FullName.LastIndexOf("`", StringComparison.InvariantCulture))}<{string.Join(", ", type.GetGenericArguments().Select(PrettyName))}>";

            return type.FullName;
        }
    }
}
