using System.Collections.Generic;
using System.Linq;

namespace ScrollerMapperTests.Services
{
    internal static class TestExtensions
    {
        public static IEnumerable<string> ToStringCollection(this IEnumerable<object> collection)
        {
            return collection.Select(_ => _.ToString());
        }
    }
}
