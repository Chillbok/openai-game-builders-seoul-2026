using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 플레이어 주변 월드 공간 HUD 중 회피 충전(단일 슬롯 + 원형 충전 표시) 표시를 담당한다.
public sealed class PlayerHUDController : MonoBehaviour
{
    [Header("플레이어 데이터")]
    [SerializeField]
    [Tooltip("HUD가 구독할 플레이어 스탯 컨트롤러")]
    private PlayerStatController playerStatController;

    [Header("회피 충전 표시")]
    [SerializeField]
    [Tooltip("회피 충전 진행도를 표시하는 레이디얼 필 이미지 (Fill Method: Radial 360, Fill Clockwise: false)")]
    private Image dodgeFillImage;

    [SerializeField]
    [Tooltip("현재 회피 충전 개수를 표시하는 텍스트")]
    private TMP_Text dodgeCountText;

    [Header("슬롯 색상")]
    [SerializeField]
    [Tooltip("회피 충전이 사용 가능한 상태의 색상")]
    private Color availableDodgeColor = Color.white;

    [SerializeField]
    [Tooltip("회피 충전이 소진된 상태의 색상")]
    private Color usedDodgeColor = new Color(1f, 1f, 1f, 0.25f);

    private bool subscribed;

    private void Start()
    {
        if (playerStatController == null)
        {
            Debug.LogError("PlayerHUDController에 PlayerStatController가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        Subscribe();
        UpdateDodgeSlot(playerStatController.CurrentDodgeCount, playerStatController.DodgeFillProgress);
    }

    private void Update()
    {
        if (!subscribed || playerStatController == null)
        {
            return;
        }

        // 매 프레임 충전 진행도 갱신 (개수는 이벤트로 갱신됨)
        if (dodgeFillImage != null)
        {
            UpdateFill(playerStatController.CurrentDodgeCount, playerStatController.DodgeFillProgress);
        }
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

        playerStatController.CurrentDodgeCountChanged += UpdateDodgeSlot;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || playerStatController == null)
        {
            return;
        }

        playerStatController.CurrentDodgeCountChanged -= UpdateDodgeSlot;
        subscribed = false;
    }

    private void UpdateDodgeSlot(int currentDodgeCount)
    {
        UpdateDodgeSlot(currentDodgeCount, playerStatController?.DodgeFillProgress ?? 1f);
    }

    private void UpdateDodgeSlot(int currentDodgeCount, float rechargeProgress)
    {
        UpdateFill(currentDodgeCount, rechargeProgress);
        UpdateCountText(currentDodgeCount);
    }

    private void UpdateFill(int currentDodgeCount, float rechargeProgress)
    {
        if (dodgeFillImage == null)
        {
            return;
        }

        dodgeFillImage.fillAmount = Mathf.Clamp01(rechargeProgress);
        dodgeFillImage.color = currentDodgeCount >= PlayerRuntimeState.MaxDodgeCount
            ? availableDodgeColor
            : usedDodgeColor;
    }

    private void UpdateCountText(int currentDodgeCount)
    {
        if (dodgeCountText == null)
        {
            return;
        }

        dodgeCountText.text = currentDodgeCount.ToString();
    }
}
