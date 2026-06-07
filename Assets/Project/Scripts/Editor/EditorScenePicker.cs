using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gazeus.DesafioMatch3.Editor
{
    public class EditorScenePicker
    {
        internal const string ElementPath = "Desafio Match 3/Scene Picker";

        private struct SceneEntry
        {
            public string Name;
            public string Path;
        }

        private static readonly List<SceneEntry> Scenes = new();

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            RefreshSceneList();
            EditorApplication.projectChanged += RefreshSceneList;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            EditorApplication.delayCall += () => MainToolbar.Refresh(ElementPath);
        }

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarElement CreateScenePicker()
        {
            string activeSceneName = Application.isPlaying
                ? SceneManager.GetActiveScene().name
                : EditorSceneManager.GetActiveScene().name;

            if (string.IsNullOrEmpty(activeSceneName))
                activeSceneName = "Untitled";

            var content = new MainToolbarContent(activeSceneName, "Switch scene");
            return new MainToolbarDropdown(content, ShowDropdownMenu)
            {
                enabled = !Application.isPlaying
            };
        }

        private static void ShowDropdownMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();

            if (Scenes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No scenes in Build Settings"));
                menu.DropDown(dropDownRect);
                return;
            }

            string activeSceneName = EditorSceneManager.GetActiveScene().name;
            foreach (SceneEntry scene in Scenes)
            {
                bool isActive = scene.Name == activeSceneName;
                menu.AddItem(new GUIContent(scene.Name), isActive, () => SwitchScene(scene.Path));
            }

            menu.DropDown(dropDownRect);
        }

        private static void SwitchScene(string scenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private static void RefreshSceneList()
        {
            Scenes.Clear();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrEmpty(scene.path) || !scene.path.StartsWith("Assets"))
                    continue;

                Scenes.Add(new SceneEntry
                {
                    Name = Path.GetFileNameWithoutExtension(scene.path),
                    Path = scene.path
                });
            }
        }

        private static void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            MainToolbar.Refresh(ElementPath);
        }
    }
}
