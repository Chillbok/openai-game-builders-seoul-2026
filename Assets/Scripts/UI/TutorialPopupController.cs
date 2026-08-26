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
        // 기존 씬에 저장된 구버전(방어 포함, F/Space 표기, 10초 기절)도 직관적인 신버전으로 교체
        bool needRefresh = pages == null || pages.Length != 3;
        if (!needRefresh)
        {
            // 구버전 식별: 제목이 "조작법"이거나 본문에 "방어:" 또는 "처형: F"가 포함된 경우
            string b0 = pages[0].body ?? "";
            if (pages[0].title == "조작법" || b0.Contains("방어:") || b0.Contains("처형: F") || b0.Contains("회피: Space"))
                needRefresh = true;
            else
            {
                bool anyEmpty = false;
                for (int i = 0; i < 3; i++)
                    if (string.IsNullOrEmpty(pages[i].title) || string.IsNullOrEmpty(pages[i].body)) anyEmpty = true;
                if (anyEmpty) needRefresh = true;
            }
        }
        if (!needRefresh) return;

        pages = new TutorialPage[3];
        pages[0] = new TutorialPage
        {
            title = "이동 & 공격 — 가장 기본!",
            body = "[W][A][S][D] 이동 — 누른 방향으로 달려요\n[좌클릭] 공격 — 이동 방향으로 3타 콤보!\n콤보는 0.5초 안에 다시 클릭하면 이어집니다\n팁: 대각선은 좌/우 중 하나로 공격 방향이 보정돼요"
        };
        pages[1] = new TutorialPage
        {
            title = "회피 & 처형 — 위기를 기회로!",
            body = "[우클릭] 회피 — 짧게 돌진하며 무적! (최대 3회, 2초마다 1회 회복)\n완벽 타이밍 회피 성공 → 다음 공격이 2배 강해집니다!\n[노란 깜빡임] 기절한 적 → [E] 처형으로 마무리!\n처형: 돌진 → 2초 연출(무적) → 체력 +25 & 영혼 충전 +1"
        };
        pages[2] = new TutorialPage
        {
            title = "강해지고 오래 살아남기",
            body = "영혼 충전: 일반 처치 4번 또는 처형 1번 = 1단계 (최대 4단계)\n1단계 피해 -30% / 2단계 공격 빨라짐 / 3단계 공격 1.5배 / 4단계 처치 시 폭발!\n12초마다 1단계씩 사라지니 계속 처치·처형하세요\n목표: 죽기 전까지 최고 점수 도전! 지속 시간 동안 버티고, 남은 적을 모두 잡으면 문이 열립니다.\n문 위에 올라가 다음 방으로 넘어가세요.\n점수: 처치 100점 / 처형 150점(100+50)"
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
