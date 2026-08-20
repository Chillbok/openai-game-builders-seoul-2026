using UnityEngine;
using UnityEngine.UI;

// 플레이어 주변 월드 공간 HUD 중 회피 충전(슬롯 3칸) 표시를 담당한다.
public sealed class PlayerHUDController : MonoBehaviour
{
    [Header("플레이어 데이터")]
    [SerializeField]
    [Tooltip("HUD가 구독할 플레이어 스탯 컨트롤러")]
    private PlayerStatController playerStatController;

    [Header("회피 충전 표시")]
    [SerializeField]
    [Tooltip("회피 충전 슬롯 이미지(개수는 PlayerRuntimeState.MaxDodgeCount와 일치해야 함)")]
    private Image[] dodgeSlotImages;

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
        UpdateDodgeSlots(playerStatController.CurrentDodgeCount);
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

        playerStatController.CurrentDodgeCountChanged += UpdateDodgeSlots;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || playerStatController == null)
        {
            return;
        }

        playerStatController.CurrentDodgeCountChanged -= UpdateDodgeSlots;
        subscribed = false;
    }

    private void UpdateDodgeSlots(int currentDodgeCount)
    {
        if (dodgeSlotImages == null)
        {
            return;
        }

        for (int i = 0; i < dodgeSlotImages.Length; i++)
        {
            if (dodgeSlotImages[i] == null)
            {
                continue;
            }

            dodgeSlotImages[i].color = i < currentDodgeCount ? availableDodgeColor : usedDodgeColor;
        }
    }
}