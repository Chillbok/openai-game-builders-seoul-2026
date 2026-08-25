using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 화면 공간 HUD 중 체력바와 영혼 충전(단계 구슬 + 처치 누적 진행도) 표시를 담당한다.
public sealed class PlayerScreenHUDController : MonoBehaviour
{
    [Header("플레이어 데이터")]
    [SerializeField]
    [Tooltip("HUD가 구독할 플레이어 스탯 컨트롤러")]
    private PlayerStatController playerStatController;

    [Header("체력바")]
    [SerializeField]
    [Tooltip("현재 체력 비율만큼 채워지는 필러 이미지")]
    private Image hpFill;

    [SerializeField]
    [Tooltip("현재/최대 체력을 표시하는 텍스트")]
    private TMP_Text hpText;

    [Header("영혼 충전")]
    [SerializeField]
    [Tooltip("영혼 충전 단계를 표시하는 핍 이미지(개수는 PlayerRuntimeState.MaxSoulChargeStage와 일치해야 함)")]
    private Image[] soulChargePips;

    [SerializeField]
    [Tooltip("다음 단계까지의 처치 누적 진행도(예: 2 / 4)를 표시하는 텍스트")]
    private TMP_Text soulChargeProgressText;

    [Header("핍 색상")]
    [SerializeField]
    [Tooltip("영혼 충전 단계가 활성화된 핍의 색상")]
    private Color chargedSoulColor = Color.white;

    [SerializeField]
    [Tooltip("영혼 충전 단계가 비어 있는 핍의 색상")]
    private Color emptySoulColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("오디오")]
    [SerializeField]
    [Tooltip("중앙 오디오 설정 — 비어 있으면 Resources/DefaultAudioConfig 또는 AudioService를 사용한다")]
    private AudioConfig audioConfig;

    [Header("점수")]
    [SerializeField]
    [Tooltip("총 점수를 표시하는 텍스트. 비어 있으면 자동 생성한다")]
    private TMP_Text totalScoreText;

    [SerializeField]
    [Tooltip("HUD가 구독할 점수 컨트롤러. 비어 있으면 씬에서 탐색한다")]
    private ScoreController scoreController;

    [Header("폰트 (중앙 관리)")]
    [Tooltip("비어 있으면 GameFontConfig 또는 TMP Settings 기본값을 사용한다")]
    [SerializeField]
    private GameFontConfig fontConfig;

    private bool subscribed;
    private int displayedSoulChargeStage = -1;

    private void Start()
    {
        if (playerStatController == null)
        {
            Debug.LogError("PlayerScreenHUDController에 PlayerStatController가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        if (scoreController == null) scoreController = FindFirstObjectByType<ScoreController>();
        if (fontConfig == null) fontConfig = Resources.Load<GameFontConfig>("GameFontConfig");
        if (fontConfig == null) fontConfig = FindFirstObjectByType<GameFontConfig>(); // fallback (씬 배치 시)
        // 에디터에서 GameFontConfig.asset 경로 로드
#if UNITY_EDITOR
        if (fontConfig == null) fontConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFontConfig>("Assets/ScriptableObjects/UI/GameFontConfig.asset");
#endif
        // 무조건 Resources/DefaultAudioConfig 사용 — 인스펙터 할당 무시
        audioConfig = Resources.Load<AudioConfig>("DefaultAudioConfig");
#if UNITY_EDITOR
        if (audioConfig == null) audioConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioConfig>("Assets/Resources/DefaultAudioConfig.asset");
#endif
        if (audioConfig == null && AudioService.Instance != null) audioConfig = AudioService.Instance.Config;
        EnsureTotalScoreText();

        Subscribe();
        UpdateHP(playerStatController.CurrentHP);
        ConfigureSoulChargePips();
        UpdateSoulChargePips(playerStatController.CurrentSoulChargeStage);
        UpdateSoulChargeProgress();
        displayedSoulChargeStage = playerStatController.CurrentSoulChargeStage;
        UpdateTotalScore();
    }

    private void Update()
    {
        if (!subscribed || playerStatController == null)
        {
            return;
        }

        UpdateSoulChargePipFill();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        playerStatController.CurrentHPChanged += UpdateHP;
        playerStatController.SoulChargeStageChanged += OnSoulChargeStageChanged;
        playerStatController.NormalKillCountChanged += OnNormalKillCountChanged;
        if (scoreController != null) scoreController.ScoreChanged += UpdateTotalScore;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (playerStatController != null)
        {
            playerStatController.CurrentHPChanged -= UpdateHP;
            playerStatController.SoulChargeStageChanged -= OnSoulChargeStageChanged;
            playerStatController.NormalKillCountChanged -= OnNormalKillCountChanged;
        }
        if (scoreController != null) scoreController.ScoreChanged -= UpdateTotalScore;
        subscribed = false;
    }

    private void UpdateHP(float currentHP)
    {
        float maxHP = playerStatController.MaxHP;
        if (hpFill != null)
        {
            hpFill.fillAmount = maxHP > 0f ? currentHP / maxHP : 0f;
        }

        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}";
        }
    }

    private void OnSoulChargeStageChanged(int stage)
    {
        PlaySoulChargeStageSound(stage);
        UpdateSoulChargePips(stage);
        UpdateSoulChargeProgress();
    }

    private void PlaySoulChargeStageSound(int stage)
    {
        // 무조건 Resources/DefaultAudioConfig — 인스펙터 할당 무시
        AudioConfig cfg = AudioService.Instance != null && AudioService.Instance.Config != null
            ? AudioService.Instance.Config
            : Resources.Load<AudioConfig>("DefaultAudioConfig");
#if UNITY_EDITOR
        if (cfg == null) cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioConfig>("Assets/Resources/DefaultAudioConfig.asset");
#endif

        AudioClip clip = null;
        if (cfg != null)
        {
            clip = stage > displayedSoulChargeStage
                ? cfg.SoulChargeStageUpClip
                : cfg.SoulChargeStageDownClip;
        }

        displayedSoulChargeStage = stage;

        if (clip == null) return;

        if (AudioService.Instance != null)
        {
            AudioService.Instance.PlaySFX(clip, priority: AudioService.Priority.Medium);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, playerStatController.transform.position);
        }
    }

    private void OnNormalKillCountChanged(int normalKillCount)
    {
        UpdateSoulChargeProgress();
    }

    private void UpdateSoulChargePips(int stage)
    {
        if (soulChargePips == null)
        {
            return;
        }

        int clampedStage = Mathf.Clamp(stage, 0, PlayerRuntimeState.MaxSoulChargeStage);
        for (int i = 0; i < soulChargePips.Length; i++)
        {
            if (soulChargePips[i] == null)
            {
                continue;
            }

            bool isActive = i < clampedStage;
            soulChargePips[i].gameObject.SetActive(isActive);
            soulChargePips[i].color = isActive ? chargedSoulColor : emptySoulColor;
        }

        UpdateSoulChargePipFill();
    }

    // 핍을 기획서의 세로 와이프 방식으로 설정한다. 남은 시간이 줄면 아래쪽부터 비워진다.
    private void ConfigureSoulChargePips()
    {
        if (soulChargePips == null)
        {
            return;
        }

        foreach (Image pip in soulChargePips)
        {
            if (pip == null)
            {
                continue;
            }

            pip.type = Image.Type.Filled;
            pip.fillMethod = Image.FillMethod.Vertical;
            pip.fillOrigin = (int)Image.OriginVertical.Bottom;
            pip.fillClockwise = false;
        }
    }

    private void UpdateSoulChargePipFill()
    {
        if (soulChargePips == null || playerStatController == null)
        {
            return;
        }

        float duration = playerStatController.SoulChargeDuration;
        float fillAmount = playerStatController.CurrentSoulChargeStage > 0 && duration > 0f
            ? Mathf.Clamp01(playerStatController.SoulChargeRemainingTime / duration)
            : 0f;

        int activePipCount = Mathf.Clamp(
            playerStatController.CurrentSoulChargeStage,
            0,
            PlayerRuntimeState.MaxSoulChargeStage);
        for (int i = 0; i < soulChargePips.Length; i++)
        {
            if (soulChargePips[i] == null || i >= activePipCount)
            {
                continue;
            }

            bool isTopPip = i == activePipCount - 1;
            soulChargePips[i].fillAmount = isTopPip ? fillAmount : 1f;
        }
    }

    private void UpdateSoulChargeProgress()
    {
        if (soulChargeProgressText == null)
        {
            return;
        }

        if (playerStatController.CurrentSoulChargeStage >= PlayerRuntimeState.MaxSoulChargeStage)
        {
            soulChargeProgressText.text = "MAX";
            return;
        }

        soulChargeProgressText.text = $"{playerStatController.NormalKillCount} / {PlayerStatController.NormalKillsRequiredForSoulCharge}";
    }

    private TMP_FontAsset ResolveFont()
    {
        if (fontConfig != null && fontConfig.DefaultFont != null) return fontConfig.DefaultFont;
        return TMP_Settings.defaultFontAsset;
    }

    private void EnsureTotalScoreText()
    {
        if (totalScoreText != null)
        {
            // 기존 인스펙터 지정 텍스트도 중앙 폰트로 보정
            if (totalScoreText.font != ResolveFont()) totalScoreText.font = ResolveFont();
            return;
        }

        // HUD에 총점 텍스트가 없으면 우상단에 자동 생성
        GameObject go = new GameObject("TotalScoreText", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(200f, 40f);

        totalScoreText = go.AddComponent<TextMeshProUGUI>();
        totalScoreText.font = ResolveFont();
        totalScoreText.fontSize = 28f;
        totalScoreText.alignment = TextAlignmentOptions.TopRight;
        totalScoreText.color = Color.white;
        totalScoreText.text = "0";
        totalScoreText.raycastTarget = false;
    }

    private void UpdateTotalScore()
    {
        if (totalScoreText == null || scoreController == null) return;
        totalScoreText.text = $"{scoreController.TotalScore}";
    }
}
