using Elmah.Io.Client;
using System.Collections.Generic;

namespace Serilog.Sinks.ElmahIo
{
    internal static class KeyValuePairExtensions
    {
        internal static Item ToItem<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair)
        {
            return new Item(Trim(keyValuePair.Key?.ToString()), Trim(keyValuePair.Value?.ToString()));
        }

        private static string Trim(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return s.TrimStart(new[] { '\"' }).TrimEnd(new[] { '\"' });
        }
    }
}
