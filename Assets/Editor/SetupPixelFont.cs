using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

// EDITOR ONLY (not part of the game): sets up the Silkscreen pixel font.
// Click Tools > Setup Pixel Font. It can be run again safely.
// 1. Creates the TextMeshPro font assets from Assets/Fonts (hard pixels, point filtering, no blur).
// 2. Makes Silkscreen Bold the default TextMeshPro font (used by UIFactory, PauseMenu, GameOverMenu...).
// 3. In the scenes, replaces the old default font (LiberationSans) by Silkscreen Bold.
//    Only the font line of each text changes, nothing else (MainMenu and Level2 belong to my teammate).
public static class SetupPixelFont
{
    static readonly string[] scenes = { "Assets/MainMenu.unity", "Assets/Level1.unity", "Assets/Level2.unity", "Assets/GameOver.unity", "Assets/Win.unity" };

    // Letters, numbers, signs + accents and the "·" of the level titles
    const string characters =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "·«»àâäçéèêëîïôöùûüÀÂÄÇÉÈÊËÎÏÔÖÙÛÜ";

    [MenuItem("Tools/Setup Pixel Font")]
    static void RunFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Run();
    }

    // Public so it can also run from the command line (batch mode)
    public static void Run()
    {
        TMP_FontAsset oldFont = TMP_Settings.defaultFontAsset;   // LiberationSans SDF
        TMP_FontAsset regular = MakeFontAsset("Assets/Fonts/Silkscreen-Regular.ttf", "Assets/Fonts/Silkscreen-Regular Pixel.asset");
        TMP_FontAsset bold = MakeFontAsset("Assets/Fonts/Silkscreen-Bold.ttf", "Assets/Fonts/Silkscreen-Bold Pixel.asset");

        // <b> and "Bold" use the real bold font (a pixel font can't be made bold by the shader)
        foreach (TMP_FontAsset f in new[] { regular, bold })
        {
            f.fontWeightTable[4].regularTypeface = regular;
            f.fontWeightTable[7].regularTypeface = bold;
            EditorUtility.SetDirty(f);
        }

        // Default font of TextMeshPro
        SerializedObject settings = new SerializedObject(TMP_Settings.instance);
        settings.FindProperty("m_defaultFontAsset").objectReferenceValue = bold;
        settings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        if (oldFont != null && oldFont != bold)
        {
            ReplaceInScenes(Ref(oldFont), Ref(bold));
            ReplaceInScenes(Ref(oldFont.material), Ref(bold.material));
        }
        AssetDatabase.Refresh();
        Debug.Log("SetupPixelFont: done");
    }

    static TMP_FontAsset MakeFontAsset(string fontPath, string assetPath)
    {
        TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (asset == null)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            // RASTER_HINTED = hard pixels (no smooth edges), 32 = 4 screen pixels per font pixel
            asset = TMP_FontAsset.CreateFontAsset(font, 32, 2, GlyphRenderMode.RASTER_HINTED, 512, 512, AtlasPopulationMode.Dynamic);
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.atlasTextures[0].name = asset.name + " Atlas";
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
        }

        string missing;
        asset.TryAddCharacters(characters, out missing);
        if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("SetupPixelFont: " + asset.name + " has no " + missing);

        // Static: the atlas doesn't change anymore while playing. Point = no blur.
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        asset.atlasTextures[0].filterMode = FilterMode.Point;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    // How a scene file writes a link to this asset, for example {fileID: 11400000, guid: 8f58..., type: 2}
    static string Ref(Object o)
    {
        string guid;
        long id;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out guid, out id);
        return "{fileID: " + id + ", guid: " + guid + ", type: 2}";
    }

    // Text replace in the scene files: the scenes are not opened and saved again by Unity,
    // so nothing else in them changes
    static void ReplaceInScenes(string from, string to)
    {
        foreach (string path in scenes)
        {
            string text = File.ReadAllText(path);
            if (!text.Contains(from)) continue;
            File.WriteAllText(path, text.Replace(from, to));
            Debug.Log("SetupPixelFont: " + path + " updated");
        }
    }
}
