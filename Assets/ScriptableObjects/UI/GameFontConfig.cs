using TMPro;
using UnityEngine;

/// <summary>
/// 게임 전역 폰트 설정 — 단일 진실 원천(Single Source of Truth).
/// 기본 한글 폰트(DungGeunMo SDF)와 영문 폴백을 중앙 관리한다.
/// </summary>
[CreateAssetMenu(menuName = "Game/Font Config", fileName = "GameFontConfig")]
public sealed class GameFontConfig : ScriptableObject
{
    [Header("폰트")]
    [Tooltip("기본 한글 폰트 (예: DungGeunMo SDF)")]
    [SerializeField]
    private TMP_FontAsset defaultFont;

    [Tooltip("영문 폴백 폰트 (예: LiberationSans SDF)")]
    [SerializeField]
    private TMP_FontAsset fallbackFont;

    public TMP_FontAsset DefaultFont => defaultFont;
    public TMP_FontAsset FallbackFont => fallbackFont;

    public TMP_FontAsset Resolve() => defaultFont != null ? defaultFont : TMP_Settings.defaultFontAsset;

    public void ApplyToTMPSettings()
    {
        if (defaultFont == null) return;
        TMP_Settings.defaultFontAsset = defaultFont;
        if (fallbackFont != null)
        {
            var fallbacks = TMP_Settings.fallbackFontAssets;
            if (!fallbacks.Contains(fallbackFont))
            {
                var list = new System.Collections.Generic.List<TMP_FontAsset>(fallbacks);
                list.Add(fallbackFont);
                TMP_Settings.fallbackFontAssets = list;
            }
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(TMP_Settings.instance);
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (defaultFont == null) return;
        UnityEditor.EditorApplication.delayCall += () => ApplyToTMPSettings();
    }
#endif
}
