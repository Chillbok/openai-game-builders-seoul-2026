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

    [Header("일반 처치 진행 팝업")]
    [SerializeField, Min(0f)]
    [Tooltip("일반 처치 진행 아이콘을 플레이어 머리 위에 표시하는 시간(초)")]
    private float soulChargeProgressPopupDuration = 1f;

    [SerializeField, Min(0f)]
    [Tooltip("플레이어 머리 위에 표시할 진행 아이콘의 높이")]
    private float soulChargeProgressPopupHeight = 45f;

    private bool subscribed;
    private Image soulChargeProgressPopup;
    private float soulChargeProgressPopupRemaining;

    private void Start()
    {
        if (playerStatController == null)
        {
            Debug.LogError("PlayerHUDController에 PlayerStatController가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        Subscribe();
        CreateSoulChargeProgressPopup();
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

        UpdateSoulChargeProgressPopup();
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
        playerStatController.NormalKillRegistered += ShowSoulChargeProgressPopup;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || playerStatController == null)
        {
            return;
        }

        playerStatController.CurrentDodgeCountChanged -= UpdateDodgeSlot;
        playerStatController.NormalKillRegistered -= ShowSoulChargeProgressPopup;
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

    // 기존 회피 아이콘과 동일한 스프라이트로 월드 공간 진행 팝업을 만든다.
    private void CreateSoulChargeProgressPopup()
    {
        if (soulChargeProgressPopup != null)
        {
            return;
        }

        GameObject popupObject = new GameObject("SoulChargeProgressPopup", typeof(RectTransform), typeof(Image));
        popupObject.transform.SetParent(transform, false);

        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = new Vector2(0f, soulChargeProgressPopupHeight);
        popupRect.sizeDelta = new Vector2(32f, 32f);

        soulChargeProgressPopup = popupObject.GetComponent<Image>();
        soulChargeProgressPopup.sprite = dodgeFillImage != null ? dodgeFillImage.sprite : null;
        soulChargeProgressPopup.type = Image.Type.Filled;
        soulChargeProgressPopup.fillMethod = Image.FillMethod.Radial360;
        soulChargeProgressPopup.fillClockwise = false;
        soulChargeProgressPopup.raycastTarget = false;
        soulChargeProgressPopup.color = availableDodgeColor;
        soulChargeProgressPopup.fillAmount = 0f;
        popupObject.SetActive(false);
    }

    // 일반 처치 횟수에 해당하는 만큼 아이콘을 반시계 방향으로 채워 1초간 보여준다.
    private void ShowSoulChargeProgressPopup(int normalKillProgress)
    {
        if (soulChargeProgressPopup == null || soulChargeProgressPopupDuration <= 0f)
        {
            return;
        }

        soulChargeProgressPopup.fillAmount = Mathf.Clamp01(
            (float)normalKillProgress / PlayerStatController.NormalKillsRequiredForSoulCharge);
        soulChargeProgressPopupRemaining = soulChargeProgressPopupDuration;
        soulChargeProgressPopup.gameObject.SetActive(true);
    }

    private void UpdateSoulChargeProgressPopup()
    {
        if (soulChargeProgressPopup == null || soulChargeProgressPopupRemaining <= 0f)
        {
            return;
        }

        soulChargeProgressPopupRemaining -= Time.deltaTime;
        if (soulChargeProgressPopupRemaining <= 0f)
        {
            soulChargeProgressPopupRemaining = 0f;
            soulChargeProgressPopup.gameObject.SetActive(false);
        }
    }
}
