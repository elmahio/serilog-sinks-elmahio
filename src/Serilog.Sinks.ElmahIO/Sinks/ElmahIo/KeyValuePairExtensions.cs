using Elmah.Io.Client;
using Serilog.Events;
using System.Collections.Generic;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Serilog.Sinks.ElmahIo
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    internal static class KeyValuePairExtensions
    {
        internal static Item ToItem<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair)
        {
            return new Item(RawValue(keyValuePair.Key), RawValue(keyValuePair.Value));
        }

        private static string RawValue(object obj)
        {
            if (obj == null) return null;

            // Handling of ScalarValue from Serilog
            if (obj is ScalarValue scalar)
            {
                return scalar.Value?.ToString();
            }

            // For other types, use ToString and treat whitespace as null
            var result = obj.ToString();

            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
    }
}
