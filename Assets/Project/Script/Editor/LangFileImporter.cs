using System.IO;
using System.Text;
using Gazeus.DesafioMatch3.Localization;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    [ScriptedImporter(2, "lang")]
    public class LangFileImporter : ScriptedImporter
    {
        private const string RegistryFileName = "Languages";

        public override void OnImportAsset(AssetImportContext context)
        {
            string text = File.ReadAllText(context.assetPath, Encoding.UTF8);
            LangEntry[] entries = LangFileParser.Parse(text);
            string fileName = Path.GetFileNameWithoutExtension(context.assetPath);

            if (fileName == RegistryFileName)
            {
                LanguageRegistryAsset registry = ScriptableObject.CreateInstance<LanguageRegistryAsset>();
                registry.SetEntries(entries);
                context.AddObjectToAsset(RegistryFileName, registry);
                context.SetMainObject(registry);
                return;
            }

            LangTableAsset table = ScriptableObject.CreateInstance<LangTableAsset>();
            table.SetEntries(entries);
            table.name = fileName;
            context.AddObjectToAsset(fileName, table);
            context.SetMainObject(table);
        }
    }
}
