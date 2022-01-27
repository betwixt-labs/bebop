using System.Collections.Generic;
using System.Linq;

namespace Core.Meta.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<(T value, int index)> Enumerated<T>(this IEnumerable<T> e) =>
            e.Select((v, i) => (v, i));
    }
}
