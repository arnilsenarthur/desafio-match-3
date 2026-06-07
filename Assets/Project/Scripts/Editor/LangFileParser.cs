using System;
using System.Collections.Generic;

namespace Gazeus.DesafioMatch3.Editor
{
    internal static class LangFileParser
    {
        public static KeyValuePair<string, string>[] Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Array.Empty<KeyValuePair<string, string>>();
            }

            List<KeyValuePair<string, string>> entries = new();
            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                entries.Add(new KeyValuePair<string, string>(
                    line.Substring(0, separator).Trim(),
                    Unescape(line.Substring(separator + 1).Trim())));
            }

            return entries.ToArray();
        }

        private static string Unescape(string value)
        {
            if (value.IndexOf('\\') < 0)
            {
                return value;
            }

            return value
                .Replace("\\n", "\n", StringComparison.Ordinal)
                .Replace("\\t", "\t", StringComparison.Ordinal);
        }
    }
}
