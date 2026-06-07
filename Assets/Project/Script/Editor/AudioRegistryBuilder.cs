using System.Collections.Generic;
using Gazeus.DesafioMatch3.Audio;
using UnityEditor;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    [InitializeOnLoad]
    public static class AudioRegistryBuilder
    {
        private const string RegistryAssetPath = "Assets/Project/Resources/Audio/AudioRegistry.asset";
        private const string LegacyRegistryAssetPath = "Assets/Project/Resources/Audio/Registry.asset";
        private const string AudioFolderPath = "Assets/Project/Audio";

        private static readonly (string FileName, string Key, bool IsMusic)[] KnownMappings =
        {
            ("AudioClick", AudioKeys.UiClick, false),
            ("AudioPopup", AudioKeys.UiPopup, false),
            ("AudioCheck", AudioKeys.UiCheck, false),
            ("AudioPause", AudioKeys.UiPause, false),
            ("AudioGameStart", AudioKeys.GameplayGameStart, false),
            ("AudioGemSelect", AudioKeys.GameplayGemSelect, false),
            ("AudioGemSlide", AudioKeys.GameplayGemSlide, false),
            ("AudioMatch", AudioKeys.GameplayMatch, false),
            ("AudioMatchSpecial", AudioKeys.GameplayMatchSpecial, false),
            ("AudioGameOver", AudioKeys.GameplayGameOver, false),
            ("AudioHighScore", AudioKeys.GameplayHighScore, false),
            ("AudioTutorialPage", AudioKeys.UiTutorialPage, false),
            ("MusicMenu", AudioKeys.MusicMainMenu, true),
            ("MusicGameplay", AudioKeys.MusicGameplay, true),
        };

        static AudioRegistryBuilder()
        {
            EditorApplication.delayCall += EnsureRegistryExists;
        }

        private static void EnsureRegistryExists()
        {
            EnsureFolder("Assets/Project/Resources/Audio");
            MigrateLegacyRegistryAsset();

            if (AssetDatabase.LoadAssetAtPath<AudioRegistryAsset>(RegistryAssetPath) != null)
            {
                return;
            }

            if (System.IO.File.Exists(RegistryAssetPath))
            {
                AssetDatabase.DeleteAsset(RegistryAssetPath);
            }

            RebuildRegistry();
        }

        private static void MigrateLegacyRegistryAsset()
        {
            if (!System.IO.File.Exists(LegacyRegistryAssetPath))
            {
                return;
            }

            if (System.IO.File.Exists(RegistryAssetPath))
            {
                AssetDatabase.DeleteAsset(LegacyRegistryAssetPath);
                return;
            }

            AssetDatabase.MoveAsset(LegacyRegistryAssetPath, RegistryAssetPath);
            AssetDatabase.Refresh();
        }

        [MenuItem("Desafio Match 3/Audio/Rebuild Audio Registry")]
        public static void RebuildRegistry()
        {
            EnsureFolder("Assets/Project/Resources/Audio");

            AudioRegistryAsset registry = AssetDatabase.LoadAssetAtPath<AudioRegistryAsset>(RegistryAssetPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<AudioRegistryAsset>();
                AssetDatabase.CreateAsset(registry, RegistryAssetPath);
            }

            Dictionary<string, AudioClip> clipsByName = LoadClipsByName();
            registry.ClearEntries();

            for (int i = 0; i < KnownMappings.Length; i++)
            {
                (string fileName, string key, bool isMusic) = KnownMappings[i];
                clipsByName.TryGetValue(fileName, out AudioClip clip);
                registry.SetEntry(key, clip, isMusic);
            }

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Rebuilt audio registry at {RegistryAssetPath}.");
        }

        [MenuItem("Desafio Match 3/Audio/Setup Audio System")]
        public static void SetupAudioSystem()
        {
            RebuildRegistry();
            Debug.Log(
                "Audio system setup complete. Ensure MainMenu and Gameplay scenes include the AudioServiceHost prefab.");
        }

        private static Dictionary<string, AudioClip> LoadClipsByName()
        {
            var clips = new Dictionary<string, AudioClip>();
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolderPath });

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    continue;
                }

                clips[clip.name] = clip;
            }

            return clips;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
