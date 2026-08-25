using UnityEditor;
using UnityEngine;
using TMPro;

/// <summary>
/// GameFontConfig와 TMP Settings를 동기화 — 단일 진실 원천 유지.
/// GameFontConfig.asset 수정 시 TMP Settings의 default/fallback을 자동 갱신한다.
/// </summary>
public sealed class GameFontConfigSync : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool needsSync = false;
        foreach (string path in importedAssets)
        {
            if (path.EndsWith("GameFontConfig.asset"))
            {
                needsSync = true;
                break;
            }
        }
        if (needsSync)
        {
            EditorApplication.delayCall += Sync;
        }
    }

    [MenuItem("Tools/Sync Game Font Config")]
    public static void Sync()
    {
        var config = AssetDatabase.LoadAssetAtPath<GameFontConfig>("Assets/ScriptableObjects/UI/GameFontConfig.asset");
        if (config == null || config.DefaultFont == null) return;

        bool dirty = false;

        if (TMP_Settings.defaultFontAsset != config.DefaultFont)
        {
            TMP_Settings.defaultFontAsset = config.DefaultFont;
            dirty = true;
            Debug.Log($"[GameFontConfigSync] TMP default font -> {config.DefaultFont.name}");
        }

        if (config.FallbackFont != null)
        {
            var fallbacks = TMP_Settings.fallbackFontAssets;
            if (!fallbacks.Contains(config.FallbackFont))
            {
                var list = new System.Collections.Generic.List<TMP_FontAsset>(fallbacks);
                list.Add(config.FallbackFont);
                TMP_Settings.fallbackFontAssets = list;
                dirty = true;
                Debug.Log($"[GameFontConfigSync] Added fallback: {config.FallbackFont.name}");
            }
        }

        if (dirty)
        {
            EditorUtility.SetDirty(TMP_Settings.instance);
            AssetDatabase.SaveAssets();
        }
    }
}
