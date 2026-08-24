using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 보라색 폭발 애니메이션 클립에 종료 이벤트를 일괄 추가한다.
// 재실행하면 기존 OnPurpleExplosionFinished 이벤트를 제거하고 다시 적용한다.
public static class PurpleExplosionAnimationEventSetup
{
    private const string ClipPath = "Assets/Animations/Effects/PurpleEffectExplosion.anim";
    private const string FunctionName = "OnPurpleExplosionFinished";

    [MenuItem("Tools/Setup/Purple Explosion Animation Events")]
    public static void AddEvent()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            Debug.LogWarning($"보라색 폭발 애니메이션 클립을 찾지 못했습니다: {ClipPath}");
            return;
        }

        List<AnimationEvent> events = new List<AnimationEvent>(AnimationUtility.GetAnimationEvents(clip));
        events.RemoveAll(e => e.functionName == FunctionName);
        events.Add(CreateEvent(FunctionName, clip.length));
        events.Sort((a, b) => a.time.CompareTo(b.time));

        AnimationUtility.SetAnimationEvents(clip, events.ToArray());

        // 일회성 폭발은 루프하지 않도록 설정
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"보라색 폭발 이벤트 적용: {clip.name} (OnPurpleExplosionFinished at {clip.length}s, loopTime=false)");
    }

    private static AnimationEvent CreateEvent(string functionName, float time)
    {
        return new AnimationEvent
        {
            functionName = functionName,
            time = time,
        };
    }
}
