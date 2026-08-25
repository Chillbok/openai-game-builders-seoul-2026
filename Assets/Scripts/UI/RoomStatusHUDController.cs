using TMPro;
using UnityEngine;

/// <summary>
/// 방 생존 타이머와 남은 적 수를 상단 중앙 HUD에 표시한다.
/// EnemyWaveSpawner의 이벤트를 구독해 초 경계/사망 시에만 텍스트를 갱신하며,
/// 이벤트 미연결 시 0.1초 폴링 폴백으로 동작한다.
/// </summary>
public sealed class RoomStatusHUDController : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("생존 타이머와 활성 적 수를 제공하는 스포너")]
    [SerializeField]
    private EnemyWaveSpawner waveSpawner;

    [Tooltip("남은 생존 시간을 MM:SS로 표시하는 텍스트 (상단 중앙)")]
    [SerializeField]
    private TMP_Text survivalTimerText;

    [Tooltip("남은 적 수를 표시하는 텍스트 (optional, 비어 있으면 생략)")]
    [SerializeField]
    private TMP_Text aliveCountText;

    [Header("포맷")]
    [Tooltip("남은 적 수 포맷. {0}에 정수 카운트가 들어간다. 예: 남은 적 {0} 또는 {0}")]
    [SerializeField]
    private string aliveCountFormat = "남은 적 {0}";

    [Tooltip("생존 완료 후 고정 표시 텍스트. 비어 있으면 00:00을 사용한다")]
    [SerializeField]
    private string survivalCompleteText = "00:00";

    [Header("폰트 (중앙 관리)")]
    [Tooltip("비어 있으면 GameFontConfig 또는 TMP Settings 기본값을 사용한다")]
    [SerializeField]
    private GameFontConfig fontConfig;

    private bool subscribed;
    private int displayedRemainingSec = -1;
    private int displayedAliveCount = -1;
    private float pollTimer;

    private void Start()
    {
        if (waveSpawner == null) waveSpawner = FindFirstObjectByType<EnemyWaveSpawner>();
        if (waveSpawner == null)
        {
            Debug.LogWarning("RoomStatusHUDController: EnemyWaveSpawner를 찾지 못했습니다.", this);
        }

        if (fontConfig == null) fontConfig = Resources.Load<GameFontConfig>("GameFontConfig");
        if (fontConfig == null) fontConfig = FindFirstObjectByType<GameFontConfig>();
#if UNITY_EDITOR
        if (fontConfig == null) fontConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFontConfig>("Assets/ScriptableObjects/UI/GameFontConfig.asset");
#endif
        ApplyFonts();

        Subscribe();
        // 초기값 1회 그리기 (null-safe)
        if (waveSpawner != null)
        {
            int sec = Mathf.CeilToInt(waveSpawner.RemainingTime);
            UpdateSurvivalTimerText(sec);
            displayedRemainingSec = sec;
            int alive = waveSpawner.AliveCount;
            UpdateAliveCountText(alive);
            displayedAliveCount = alive;
        }
        else
        {
            UpdateSurvivalTimerText(0);
            UpdateAliveCountText(0);
        }
    }

    private void Update()
    {
        if (waveSpawner == null) return;

        // 이벤트 기반 갱신 외에 0.1초 throttling 폴링으로 보정 (중복 string 생성 방지)
        pollTimer += Time.deltaTime;
        if (pollTimer < 0.1f) return;
        pollTimer = 0f;

        int sec = Mathf.CeilToInt(waveSpawner.RemainingTime);
        // 생존 완료 시에는 고정 텍스트 유지를 위해 초 경계와 무관하게 00:00로 보정 가능
        // 하지만 RemainingTime이 0이면 sec=0으로 동일하므로 추가 분기 불필요
        if (sec != displayedRemainingSec)
        {
            displayedRemainingSec = sec;
            UpdateSurvivalTimerText(sec);
        }

        int alive = waveSpawner.AliveCount;
        if (alive != displayedAliveCount)
        {
            displayedAliveCount = alive;
            UpdateAliveCountText(alive);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed) return;
        if (waveSpawner == null) return;
        waveSpawner.SurvivalRemainingChanged += OnSurvivalRemainingChanged;
        waveSpawner.SurvivalCompleted += OnSurvivalCompleted;
        waveSpawner.AliveCountChanged += OnAliveCountChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (waveSpawner != null)
        {
            waveSpawner.SurvivalRemainingChanged -= OnSurvivalRemainingChanged;
            waveSpawner.SurvivalCompleted -= OnSurvivalCompleted;
            waveSpawner.AliveCountChanged -= OnAliveCountChanged;
        }
        subscribed = false;
    }

    private void OnSurvivalRemainingChanged(float remaining, float duration)
    {
        // null-safe: PlayerScreenHUDController 패턴 모방
        int sec = Mathf.CeilToInt(remaining);
        if (sec < 0) sec = 0;
        if (sec == displayedRemainingSec && survivalTimerText != null && survivalTimerText.text == FormatTime(sec)) return;
        displayedRemainingSec = sec;
        UpdateSurvivalTimerText(sec);
        // 폴링 타이머 리셋해 중복 갱신 방지
        pollTimer = 0f;
    }

    private void OnSurvivalCompleted()
    {
        displayedRemainingSec = 0;
        UpdateSurvivalTimerText(0, forceCompleteText: true);
        pollTimer = 0f;
    }

    private void OnAliveCountChanged(int alive)
    {
        if (alive < 0) alive = 0;
        if (alive == displayedAliveCount) return;
        displayedAliveCount = alive;
        UpdateAliveCountText(alive);
        pollTimer = 0f;
    }

    private void UpdateSurvivalTimerText(int totalSeconds, bool forceCompleteText = false)
    {
        if (survivalTimerText == null) return;
        string text;
        // 생존 완료 후에는 survivalCompleteText를 사용 (비어 있으면 00:00)
        if (forceCompleteText || (waveSpawner != null && waveSpawner.IsSurvivalComplete))
        {
            if (!string.IsNullOrEmpty(survivalCompleteText))
                text = survivalCompleteText;
            else
                text = "00:00";
        }
        else
        {
            text = FormatTime(totalSeconds);
        }
        survivalTimerText.text = text;
    }

    private void UpdateAliveCountText(int alive)
    {
        if (aliveCountText == null) return;
        string fmt = string.IsNullOrEmpty(aliveCountFormat) ? "{0}" : aliveCountFormat;
        try
        {
            aliveCountText.text = string.Format(fmt, alive);
        }
        catch
        {
            aliveCountText.text = alive.ToString();
        }
    }

    private static string FormatTime(int totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        int m = totalSeconds / 60;
        int s = totalSeconds % 60;
        return $"{m:00}:{s:00}";
    }

    private TMP_FontAsset ResolveFont()
    {
        if (fontConfig != null && fontConfig.DefaultFont != null) return fontConfig.DefaultFont;
        return TMP_Settings.defaultFontAsset;
    }

    private void ApplyFonts()
    {
        var font = ResolveFont();
        if (font == null) return;
        if (survivalTimerText != null && survivalTimerText.font != font) survivalTimerText.font = font;
        if (aliveCountText != null && aliveCountText.font != font) aliveCountText.font = font;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(aliveCountFormat)) aliveCountFormat = "{0}";
        if (waveSpawner == null) waveSpawner = FindFirstObjectByType<EnemyWaveSpawner>();
        // Text 참조가 비어 있으면 자식에서 탐색 시도 (에디터 편의)
        if (survivalTimerText == null) survivalTimerText = GetComponentInChildren<TMP_Text>();
        if (fontConfig == null) fontConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFontConfig>("Assets/ScriptableObjects/UI/GameFontConfig.asset");
        // 에디터에서 즉시 폰트 동기화 (씬 뷰 확인용)
        var f = ResolveFont();
        if (f != null)
        {
            if (survivalTimerText != null && survivalTimerText.font != f) survivalTimerText.font = f;
            if (aliveCountText != null && aliveCountText.font != f) aliveCountText.font = f;
        }
    }
#endif
}
