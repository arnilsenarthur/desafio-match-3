using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Localization
{
    public static class LocalizationService
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootstrapBeforeSceneLoad() => Initialize();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            Tables.Clear();
            Labels.Clear();
            _registry = null;
            _initialized = false;
            CurrentLanguage = LanguageCodes.Default;
            LanguageChanged = null;
        }

        private const string ResourcesFolder = "Localization";
        private const string LangFileExtension = "lang";
        private const string LanguageRegistryFileName = "Languages";

        private static readonly Dictionary<string, LangTableAsset> Tables = new();
        private static readonly HashSet<Label> Labels = new();

        private static LanguageRegistryAsset _registry;
        private static bool _initialized;

        public static event Action LanguageChanged;

        public static string CurrentLanguage { get; private set; } = LanguageCodes.Default;

        public static void Initialize(string defaultLanguage = LanguageCodes.Default)
        {
            if (_initialized)
            {
                return;
            }

            Tables.Clear();
            _registry = Resources.Load<LanguageRegistryAsset>($"{ResourcesFolder}/{LanguageRegistryFileName}");

            if (_registry == null)
            {
                Debug.LogWarning(
                    $"Missing Resources/{ResourcesFolder}/{LanguageRegistryFileName}.{LangFileExtension} (language registry).");
            }

            LangTableAsset[] tables = Resources.LoadAll<LangTableAsset>(ResourcesFolder);
            for (int i = 0; i < tables.Length; i++)
            {
                LangTableAsset table = tables[i];
                if (table == null || string.IsNullOrWhiteSpace(table.name))
                {
                    continue;
                }

                Tables[table.name] = table;
            }

            if (Tables.Count == 0)
            {
                Debug.LogError($"No .{LangFileExtension} locale files found under Resources/{ResourcesFolder}.");
            }

            _initialized = true;
            ApplyLanguage(defaultLanguage);
        }

        public static void SetLanguage(string languageCode)
        {
            EnsureInitialized();
            ApplyLanguage(languageCode);
        }

        public static IReadOnlyList<string> GetLanguageIds()
        {
            EnsureInitialized();
            var ids = new List<string>(Tables.Count);
            foreach (string id in Tables.Keys)
            {
                ids.Add(id);
            }

            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        public static IReadOnlyList<string> GetKeys()
        {
            EnsureInitialized();
            var keySet = new HashSet<string>(StringComparer.Ordinal);

            foreach (LangTableAsset table in Tables.Values)
            {
                foreach (string key in table.GetKeys())
                {
                    keySet.Add(key);
                }
            }

            var keys = new List<string>(keySet);
            keys.Sort(StringComparer.Ordinal);
            return keys;
        }

        public static string Localize(string key, params object[] args)
        {
            EnsureInitialized();

            if (!TryGetEntry(CurrentLanguage, key, out string template))
            {
                Debug.LogWarning($"Missing localization key '{key}' for '{CurrentLanguage}'.");
                return FormatMissingKey(key, args);
            }

            if (args == null || args.Length == 0)
            {
                return template;
            }

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException exception)
            {
                Debug.LogError($"Format error for key '{key}': {exception.Message}");
                return template;
            }
        }

        public static string GetLanguageDisplayName(string languageCode)
        {
            EnsureInitialized();

            if (_registry != null && _registry.TryGetDisplayName(languageCode, out string displayName))
            {
                return displayName;
            }

            return languageCode;
        }

        public static string LocalizeDifficulty(string difficultyId) =>
            Localize($"difficulty.{difficultyId}");

        internal static void Register(Label label)
        {
            if (label == null)
            {
                return;
            }

            PruneDestroyedLabels();
            Labels.Add(label);
            label.ApplyText();
        }

        internal static void Unregister(Label label)
        {
            if (label == null)
            {
                return;
            }

            Labels.Remove(label);
        }

        private static void ApplyLanguage(string languageCode)
        {
            if (!Tables.ContainsKey(languageCode))
            {
                Debug.LogWarning($"Language '{languageCode}' is not loaded.");
                return;
            }

            if (CurrentLanguage == languageCode)
            {
                return;
            }

            CurrentLanguage = languageCode;
            RefreshAllLabels();
            LanguageChanged?.Invoke();
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        private static void RefreshAllLabels()
        {
            PruneDestroyedLabels();

            foreach (Label label in Labels)
            {
                label.ApplyText();
            }
        }

        private static void PruneDestroyedLabels()
        {
            if (Labels.Count == 0)
            {
                return;
            }

            Labels.RemoveWhere(label => label == null);
        }

        private static bool TryGetEntry(string languageCode, string key, out string value)
        {
            value = null;

            if (!Tables.TryGetValue(languageCode, out LangTableAsset table))
            {
                return false;
            }

            return table.TryGet(key, out value);
        }

        private static string FormatMissingKey(string key, object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return $"missing:{key}";
            }

            return $"missing:{key}[{string.Join(", ", args)}]";
        }
    }
}
