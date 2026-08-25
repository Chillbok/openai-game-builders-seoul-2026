using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시작 메뉴 튜토리얼 팝업 — 3페이지 교체, 인디케이터, 열림/닫힘, Tutorial/Seen 저장.
/// </summary>
public sealed class TutorialPopupController : MonoBehaviour
{
    private const string PrefsSeen = "Tutorial/Seen";

    [System.Serializable]
    public struct TutorialPage
    {
        public string title;
        [TextArea(2, 6)]
        public string body;
    }

    [Header("데이터")]
    [Tooltip("3페이지 데이터 — 비어 있으면 코드 기본값 사용")]
    [SerializeField]
    private TutorialPage[] pages = new TutorialPage[3];

    [Header("참조")]
    [SerializeField]
    private GameObject popupRoot;

    [SerializeField]
    private TMP_Text pageTitleText;

    [SerializeField]
    private TMP_Text pageBodyText;

    [SerializeField]
    private TMP_Text pageNumberText;

    [SerializeField]
    private Image[] pageIndicator = new Image[3];

    [SerializeField]
    private Button prevButton;

    [SerializeField]
    private Button nextButton;

    [SerializeField]
    private Button closeButton;

    [SerializeField]
    private Button reopenButton;

    [Header("폰트")]
    [SerializeField]
    private GameFontConfig fontConfig;

    private int currentPage;
    private bool isOpen;

    public bool IsOpen => isOpen && popupRoot != null && popupRoot.activeSelf;
    public int CurrentPage => currentPage;
    public int PageCount => pages != null && pages.Length > 0 ? pages.Length : 3;

    public System.Action<bool> OnVisibilityChanged;

    private void Awake()
    {
        if (popupRoot == null) popupRoot = gameObject;
        EnsureDefaultPages();
        BindButtons();
        ApplyFonts();
        UpdateUI();
    }

    private bool autoInitialized;

    private void Start()
    {
        // 진입 시 자동 표시: Seen 없으면 열기, 있으면 닫기 — 최초 1회만
        if (autoInitialized) return;
        autoInitialized = true;
        bool seen = PlayerPrefs.GetInt(PrefsSeen, 0) == 1;
        if (!seen)
        {
            Show(0);
        }
        else
        {
            Hide();
        }
    }

    private void Update()
    {
        if (!isOpen) return;
        // ESC 닫기
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    public void Show(int pageIndex = 0)
    {
        currentPage = Mathf.Clamp(pageIndex, 0, PageCount - 1);
        isOpen = true;
        if (popupRoot != null) popupRoot.SetActive(true);
        UpdateUI();
        OnVisibilityChanged?.Invoke(true);
    }

    public void Hide()
    {
        isOpen = false;
        if (popupRoot != null) popupRoot.SetActive(false);
        OnVisibilityChanged?.Invoke(false);
    }

    public void Close()
    {
        Hide();
        PlayerPrefs.SetInt(PrefsSeen, 1);
        PlayerPrefs.Save();
    }

    public void Reopen()
    {
        Show(0);
    }

    public void Next()
    {
        if (!isOpen) return;
        if (currentPage >= PageCount - 1)
        {
            Close();
            return;
        }
        currentPage++;
        UpdateUI();
    }

    public void Prev()
    {
        if (!isOpen) return;
        if (currentPage <= 0) return;
        currentPage--;
        UpdateUI();
    }

    private void BindButtons()
    {
        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(Prev);
        }
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(Next);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
        if (reopenButton != null)
        {
            reopenButton.onClick.RemoveAllListeners();
            reopenButton.onClick.AddListener(Reopen);
        }
    }

    private void EnsureDefaultPages()
    {
        bool needDefault = pages == null || pages.Length != 3;
        if (!needDefault)
        {
            bool anyEmpty = false;
            for (int i = 0; i < 3; i++)
            {
                if (string.IsNullOrEmpty(pages[i].title) || string.IsNullOrEmpty(pages[i].body)) anyEmpty = true;
            }
            if (!anyEmpty) return;
        }

        pages = new TutorialPage[3];
        pages[0] = new TutorialPage
        {
            title = "조작법",
            body = "이동: W / A / S / D\n공격: 좌클릭 (이동 방향으로 공격)\n회피: Space (이동 방향, 완벽 회피 시 시간 감속)\n방어: 우클릭\n처형: F"
        };
        pages[1] = new TutorialPage
        {
            title = "게임 메커니즘",
            body = "체력 10% 이하 10초간 기절 → 처형 가능\n처형: 회피 거리 2배 이내 가장 가까운 대상에게 돌진\n처형 연출 2초 동안 무적\n보상: 체력 25 회복, 영혼 충전 1단계"
        };
        pages[2] = new TutorialPage
        {
            title = "게임 목표",
            body = "한 판은 사망까지 생존 — 승리 조건 없음\n방 생존 타이머: 30초 + 맵 인덱스 × 5초\n5초마다 6마리 생성, 남은 적 전멸 시 문 개방\n점수: 처치 100점 + 처형 50점"
        };
    }

    private void UpdateUI()
    {
        if (pages == null || pages.Length == 0) EnsureDefaultPages();
        int idx = Mathf.Clamp(currentPage, 0, pages.Length - 1);
        TutorialPage p = pages[idx];

        if (pageTitleText != null) pageTitleText.text = p.title;
        if (pageBodyText != null) pageBodyText.text = p.body;
        if (pageNumberText != null) pageNumberText.text = $"{idx + 1} / {pages.Length}";

        for (int i = 0; i < pageIndicator.Length; i++)
        {
            if (pageIndicator[i] == null) continue;
            bool active = i == idx;
            Color c = pageIndicator[i].color;
            // 활성: 불투명 노랑, 비활성: 반투명
            if (active)
            {
                c = new Color(0.95f, 0.87f, 0.55f, 1f);
            }
            else
            {
                c = new Color(1f, 1f, 1f, 0.35f);
            }
            pageIndicator[i].color = c;
            // 크기 강조
            pageIndicator[i].transform.localScale = active ? Vector3.one * 1.15f : Vector3.one;
        }

        if (prevButton != null) prevButton.interactable = idx > 0;
        if (nextButton != null)
        {
            // 마지막 페이지에서는 텍스트를 닫기로 표시하되 기능은 Close
            var txt = nextButton.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = idx >= pages.Length - 1 ? "닫기" : "다음";
        }
    }

    private void ApplyFonts()
    {
        TMP_FontAsset font = ResolveFont();
        if (font == null) return;
        if (pageTitleText != null && pageTitleText.font != font) pageTitleText.font = font;
        if (pageBodyText != null && pageBodyText.font != font) pageBodyText.font = font;
        if (pageNumberText != null && pageNumberText.font != font) pageNumberText.font = font;
        // 버튼 텍스트들도 폰트 통일
        if (prevButton != null)
        {
            var t = prevButton.GetComponentInChildren<TMP_Text>();
            if (t != null && t.font != font) t.font = font;
        }
        if (nextButton != null)
        {
            var t = nextButton.GetComponentInChildren<TMP_Text>();
            if (t != null && t.font != font) t.font = font;
        }
        if (closeButton != null)
        {
            var t = closeButton.GetComponentInChildren<TMP_Text>();
            if (t != null && t.font != font) t.font = font;
        }
        if (reopenButton != null)
        {
            var t = reopenButton.GetComponentInChildren<TMP_Text>();
            if (t != null && t.font != font) t.font = font;
        }
    }

    private TMP_FontAsset ResolveFont()
    {
        if (fontConfig != null && fontConfig.DefaultFont != null) return fontConfig.DefaultFont;
        fontConfig = Resources.Load<GameFontConfig>("GameFontConfig");
#if UNITY_EDITOR
        if (fontConfig == null) fontConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFontConfig>("Assets/ScriptableObjects/UI/GameFontConfig.asset");
#endif
        if (fontConfig != null && fontConfig.DefaultFont != null) return fontConfig.DefaultFont;
        return TMP_Settings.defaultFontAsset;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (popupRoot == null) popupRoot = gameObject;
        EnsureDefaultPages();
        ApplyFonts();
    }
#endif
}
