using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 공격 애니메이션 클립에 히트박스 on/off 애니메이션 이벤트를 일괄 추가한다.
// 재실행하면 기존 히트박스 이벤트를 제거하고 기본 타이밍으로 다시 적용한다.
public static class AttackHitboxAnimationEventSetup
{
    private const string EnableFunctionName = "EnableHitbox";
    private const string DisableFunctionName = "DisableHitbox";

    private static readonly string[] ClipPaths =
    {
        "Assets/Animations/Player/RightAttack/player_attack_right_2.anim",
        "Assets/Animations/Player/RightAttack/player_attack_right_3.anim",
        "Assets/Animations/Player/UpAttack/player_attack_up_1.anim",
        "Assets/Animations/Player/UpAttack/player_attack_up_2.anim",
        "Assets/Animations/Player/UpAttack/player_attack_up_3.anim",
        "Assets/Animations/Player/DownAttack/player_attack_down_1.anim",
        "Assets/Animations/Player/DownAttack/player_attack_down_2.anim",
        "Assets/Animations/Player/DownAttack/player_attack_down_3.anim",
    };

    // 클립 순서에 대응하는 히트박스 활성화(Enable) 시점. 비활성화는 클립 끝에 배치한다.
    private static readonly float[] EnableTimes =
    {
        0.25f,
        0.3333f,
        0.25f,
        0.3333f,
        0.25f,
        0.25f,
        0.3333f,
        0.25f,
    };

    [MenuItem("Tools/Setup/Attack Hitbox Animation Events")]
    public static void AddHitboxAnimationEvents()
    {
        for (int i = 0; i < ClipPaths.Length; i++)
        {
            string clipPath = ClipPaths[i];
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                Debug.LogWarning($"공격 애니메이션 클립을 찾지 못했습니다: {clipPath}");
                continue;
            }

            List<AnimationEvent> events = new List<AnimationEvent>(AnimationUtility.GetAnimationEvents(clip));
            events.RemoveAll(IsHitboxEvent);

            events.Add(CreateEvent(EnableFunctionName, EnableTimes[i]));
            events.Add(CreateEvent(DisableFunctionName, clip.length));
            events.Sort((a, b) => a.time.CompareTo(b.time));

            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
            EditorUtility.SetDirty(clip);

            Debug.Log($"히트박스 이벤트 적용: {clip.name} (Enable {EnableTimes[i]}s, Disable {clip.length}s)");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static bool IsHitboxEvent(AnimationEvent animationEvent)
    {
        return animationEvent.functionName == EnableFunctionName
            || animationEvent.functionName == DisableFunctionName;
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