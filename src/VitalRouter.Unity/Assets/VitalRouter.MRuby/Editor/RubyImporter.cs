using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace VitalRouter.MRuby.Editor
{
    [ScriptedImporter(1, "rb")]
    public class RubyImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var text = File.ReadAllText(ctx.assetPath);
            var textAsset = new TextAsset(text);
            ctx.AddObjectToAsset("Main", textAsset);
            ctx.SetMainObject(textAsset);
       }
    }
}