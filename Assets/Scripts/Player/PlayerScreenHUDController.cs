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

    private bool subscribed;

    private void Start()
    {
        if (playerStatController == null)
        {
            Debug.LogError("PlayerScreenHUDController에 PlayerStatController가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        Subscribe();
        UpdateHP(playerStatController.CurrentHP);
        UpdateSoulChargePips(playerStatController.CurrentSoulChargeStage);
        UpdateSoulChargeProgress();
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
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || playerStatController == null)
        {
            return;
        }

        playerStatController.CurrentHPChanged -= UpdateHP;
        playerStatController.SoulChargeStageChanged -= OnSoulChargeStageChanged;
        playerStatController.NormalKillCountChanged -= OnNormalKillCountChanged;
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
        UpdateSoulChargePips(stage);
        UpdateSoulChargeProgress();
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

        for (int i = 0; i < soulChargePips.Length; i++)
        {
            if (soulChargePips[i] == null)
            {
                continue;
            }

            soulChargePips[i].color = i < stage ? chargedSoulColor : emptySoulColor;
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
}