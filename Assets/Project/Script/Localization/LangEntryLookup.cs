using System;
using System.Collections.Generic;

namespace Gazeus.DesafioMatch3.Localization
{
    internal static class LangEntryLookup
    {
        public static Dictionary<string, string> Build(LangEntry[] entries)
        {
            LangEntry[] safeEntries = entries ?? Array.Empty<LangEntry>();
            var lookup = new Dictionary<string, string>(safeEntries.Length, StringComparer.Ordinal);

            for (int i = 0; i < safeEntries.Length; i++)
            {
                LangEntry entry = safeEntries[i];
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    lookup[entry.Key] = entry.Value;
                }
            }

            return lookup;
        }
    }
}
