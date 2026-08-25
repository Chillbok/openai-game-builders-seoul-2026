using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 시작 메뉴 컨트롤러 — BGM 첫 제스처 재생, 슬라이더 바인딩, 시작 입력 필터, 씬 전이, 포커스 처리.
/// </summary>
public sealed class StartMenuController : MonoBehaviour
{
    private const string PrefsBgm = "Audio/BGM";
    private const float DefaultBgmVolume = 0.8f;

    [Header("참조")]
    [SerializeField]
    private Slider bgmSlider;

    [SerializeField]
    private Button reopenButton;

    [SerializeField]
    private TMP_Text bottomHint;

    [SerializeField]
    private TutorialPopupController tutorialPopup;

    [SerializeField]
    private CanvasGroup bottomHintGroup;

    [Header("전이")]
    [Tooltip("페이드/크로스페이드 시간(초)")]
    [SerializeField, Min(0.05f)]
    private float fadeDuration = 0.3f;

    [Tooltip("하단 문구 펄스 주기(초)")]
    [SerializeField, Min(0.1f)]
    private float hintPulseDuration = 1.2f;

    [Header("폰트")]
    [SerializeField]
    private GameFontConfig fontConfig;

    private bool hasUnlockedBgm;
    private bool isTransitioning;
    private bool isDraggingSlider;
    private CanvasGroup reusableBlockerGroup;

    private void Awake()
    {
        CacheRefs();
        BindSlider();
        ApplyFonts();
    }

    private void Start()
    {
        InitBgmSlider();
        if (tutorialPopup != null)
        {
            tutorialPopup.OnVisibilityChanged += OnPopupVisibilityChanged;
            OnPopupVisibilityChanged(tutorialPopup.IsOpen);
        }
        else
        {
            UpdateHintVisibility(false);
        }
    }

    private void OnDestroy()
    {
        if (tutorialPopup != null) tutorialPopup.OnVisibilityChanged -= OnPopupVisibilityChanged;
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void Update()
    {
        HandleBgmUnlock();
        UpdateHintPulse();
        HandlePopupInputs();
        HandleStartInputs();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            isDraggingSlider = false;
        }
    }

    private void CacheRefs()
    {
        if (tutorialPopup == null) tutorialPopup = FindFirstObjectByType<TutorialPopupController>();
        if (bottomHintGroup == null && bottomHint != null) bottomHintGroup = bottomHint.GetComponent<CanvasGroup>();
        // reopenButton은 TutorialPopupController가 이미 바인딩하지만, 시작 입력 제외 판정에 필요하므로 참조 유지
    }

    private void BindSlider()
    {
        if (bgmSlider == null) return;
        bgmSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        bgmSlider.onValueChanged.AddListener(OnSliderValueChanged);

        // 드래그 감지 — EventTrigger 추가
        var trigger = bgmSlider.GetComponent<EventTrigger>();
        if (trigger == null) trigger = bgmSlider.gameObject.AddComponent<EventTrigger>();

        var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        entryDown.callback.AddListener(_ => isDraggingSlider = true);
        var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        entryUp.callback.AddListener(_ => isDraggingSlider = false);
        var entryPointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entryPointerDown.callback.AddListener(_ => isDraggingSlider = true);
        var entryPointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        entryPointerUp.callback.AddListener(_ => isDraggingSlider = false);

        // 중복 추가 방지
        bool hasBeginDrag = false;
        foreach (var e in trigger.triggers) if (e.eventID == EventTriggerType.BeginDrag) hasBeginDrag = true;
        if (!hasBeginDrag)
        {
            trigger.triggers.Add(entryDown);
            trigger.triggers.Add(entryUp);
            trigger.triggers.Add(entryPointerDown);
            trigger.triggers.Add(entryPointerUp);
        }
    }

    private void InitBgmSlider()
    {
        if (bgmSlider == null) return;
        float vol = DefaultBgmVolume;
        if (PlayerPrefs.HasKey(PrefsBgm))
        {
            vol = PlayerPrefs.GetFloat(PrefsBgm, DefaultBgmVolume);
        }
        else if (AudioService.Instance != null)
        {
            vol = AudioService.Instance.BgmVolume;
            // 저장된 값이 없을 때 Config 기본(0.254)이 아닌 기획 기본 0.8을 우선
            // 저장된 키가 없으면 현재 AudioService 값이 Config 값일 가능성이 높으므로, 0.8로 보정
            if (Mathf.Abs(vol - 0.254f) < 0.01f) vol = DefaultBgmVolume;
        }
        else
        {
            var cfg = Resources.Load<AudioConfig>("DefaultAudioConfig");
#if UNITY_EDITOR
            if (cfg == null) cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioConfig>("Assets/Resources/DefaultAudioConfig.asset");
#endif
            if (cfg != null && Mathf.Abs(cfg.BgmVolume - 0.254f) < 0.01f) vol = DefaultBgmVolume;
            else if (cfg != null) vol = cfg.BgmVolume;
        }
        vol = Mathf.Clamp01(vol);
        bgmSlider.minValue = 0f;
        bgmSlider.maxValue = 1f;
        bgmSlider.SetValueWithoutNotify(vol);
        // 초기 볼륨을 AudioService에 반영 (아직 BGM 재생 전이어도 저장 유지)
        if (AudioService.Instance != null && Mathf.Abs(AudioService.Instance.BgmVolume - vol) > 0.001f)
        {
            AudioService.Instance.SetBgmVolume(vol);
        }
    }

    private void OnSliderValueChanged(float v)
    {
        v = Mathf.Clamp01(v);
        if (AudioService.TryGetInstance(out var audio))
        {
            audio.SetBgmVolume(v);
        }
        else
        {
            PlayerPrefs.SetFloat(PrefsBgm, v);
            PlayerPrefs.Save();
        }
        TryUnlockBgm();
    }

    private void HandleBgmUnlock()
    {
        if (hasUnlockedBgm) return;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var ms = UnityEngine.InputSystem.Mouse.current;
        bool pressed = false;
        if (kb != null && kb.anyKey.wasPressedThisFrame) pressed = true;
        else if (ms != null && ms.leftButton.wasPressedThisFrame) pressed = true;

        if (pressed)
        {
            TryUnlockBgm();
        }
    }

    private void TryUnlockBgm()
    {
        if (hasUnlockedBgm) return;
        AudioConfig cfg = null;
        if (AudioService.TryGetInstance(out var audio) && audio.Config != null) cfg = audio.Config;
        else cfg = Resources.Load<AudioConfig>("DefaultAudioConfig");
#if UNITY_EDITOR
        if (cfg == null) cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioConfig>("Assets/Resources/DefaultAudioConfig.asset");
#endif
        if (cfg == null || cfg.MenuBgm == null) return;
        if (!AudioService.TryGetInstance(out var svc) || svc == null) return;
        svc.PlayBGM(cfg.MenuBgm, loop: true, fadeDuration: fadeDuration);
        hasUnlockedBgm = true;
    }

    private void HandlePopupInputs()
    {
        if (tutorialPopup == null || !tutorialPopup.IsOpen) return;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        // Enter/Space는 팝업에서 다음 페이지/닫기
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
        {
            tutorialPopup.Next();
        }
    }

    private void HandleStartInputs()
    {
        if (isTransitioning) return;
        if (tutorialPopup != null && tutorialPopup.IsOpen) return;

        var kb = UnityEngine.InputSystem.Keyboard.current;
        var ms = UnityEngine.InputSystem.Mouse.current;

        // 키보드 Enter/Space 시작
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
        {
            // 팝업 닫힘 상태이므로 바로 시작
            TryStartGame();
            return;
        }

        // 마우스 좌클릭 시작 — 슬라이더/재오픈 버튼 제외
        if (ms != null && ms.leftButton.wasPressedThisFrame)
        {
            if (isDraggingSlider) return;
            if (IsPointerOverExcludedUI()) return;
            TryStartGame();
        }
    }

    private bool IsPointerOverExcludedUI()
    {
        if (EventSystem.current == null) return false;
        // Slider_BGM과 Button_ReopenTutorial 위인지 Raycast로 판정
        var ped = new PointerEventData(EventSystem.current);
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null) ped.position = mouse.position.ReadValue();
        else ped.position = Input.mousePosition;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        foreach (var r in results)
        {
            if (r.gameObject == null) continue;
            if (bgmSlider != null && r.gameObject.transform.IsChildOf(bgmSlider.transform)) return true;
            if (bgmSlider != null && r.gameObject == bgmSlider.gameObject) return true;
            if (reopenButton != null && r.gameObject.transform.IsChildOf(reopenButton.transform)) return true;
            if (reopenButton != null && r.gameObject == reopenButton.gameObject) return true;
            // 핸들이 별도 오브젝트일 수 있으므로 이름으로도 방어
            if (bgmSlider != null && r.gameObject.name.Contains("Handle")) return true;
        }
        return false;
    }

    private void OnPopupVisibilityChanged(bool isOpen)
    {
        UpdateHintVisibility(isOpen);
    }

    private void UpdateHintVisibility(bool popupOpen)
    {
        if (bottomHint == null) return;
        bool show = !popupOpen && !isTransitioning;
        bottomHint.gameObject.SetActive(show);
        if (bottomHintGroup != null) bottomHintGroup.alpha = show ? 1f : 0f;
    }

    private void UpdateHintPulse()
    {
        if (bottomHint == null || tutorialPopup != null && tutorialPopup.IsOpen) return;
        if (isTransitioning) return;
        // 0.4 ↔ 1.0 펄스
        float t = Time.unscaledTime;
        float phase = Mathf.PingPong(t * (2f / hintPulseDuration), 1f);
        float alpha = Mathf.Lerp(0.4f, 1f, phase);
        if (bottomHintGroup != null) bottomHintGroup.alpha = alpha;
        else
        {
            Color c = bottomHint.color;
            c.a = alpha;
            bottomHint.color = c;
        }
    }

    public void TryStartGame()
    {
        if (isTransitioning) return;
        if (tutorialPopup != null && tutorialPopup.IsOpen) return;
        if (isDraggingSlider) return;
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        isTransitioning = true;
        UpdateHintVisibility(true); // hide hint

        // UI 입력 차단
        SetUiInteractable(false);

        // BGM 크로스페이드: menu → battle
        AudioConfig cfg = null;
        if (AudioService.TryGetInstance(out var svc) && svc.Config != null) cfg = svc.Config;
        else cfg = Resources.Load<AudioConfig>("DefaultAudioConfig");
#if UNITY_EDITOR
        if (cfg == null) cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioConfig>("Assets/Resources/DefaultAudioConfig.asset");
#endif
        if (cfg != null && cfg.BattleBgm != null && AudioService.TryGetInstance(out var audio2) && audio2 != null)
        {
            // 첫 제스처가 아직 없었다면 menuBgm이 무음이므로 battleBgm을 바로 재생
            audio2.PlayBGM(cfg.BattleBgm, loop: true, fadeDuration: fadeDuration);
        }

        // 페이드 아웃 → 로드 → 페이드 인은 DontDestroy인 ScreenFadeController에서 실행해야
        // StartMenu 씬이 Single로 언로드되어 이 코루틴이 중간에 파괴되는 것을 방지한다.
        var fade = ScreenFadeController.Instance;
        if (fade == null) fade = FindFirstObjectByType<ScreenFadeController>();
        if (fade == null)
        {
            var go = new GameObject("ScreenFade");
            fade = go.AddComponent<ScreenFadeController>();
        }

        // 로드 실패 시 UI 복구를 위해 콜백에서 상태 초기화
        fade.FadeOutLoadAndIn("SampleScene", success =>
        {
            if (success) return;
            Debug.LogError("StartMenuController: SampleScene 로드 실패 — FadeIn 복구 완료", this);
            if (this == null) return;
            // 씬이 유지된 경우에만 UI 복구 (성공 시 이 객체는 파괴됨)
            isTransitioning = false;
            SetUiInteractable(true);
            UpdateHintVisibility(tutorialPopup != null && tutorialPopup.IsOpen);
        });

        yield break;
    }

    private void SetUiInteractable(bool interactable)
    {
        if (bgmSlider != null) bgmSlider.interactable = interactable;
        if (reopenButton != null) reopenButton.interactable = interactable;
        if (tutorialPopup != null)
        {
            // 팝업 버튼들도 차단
            var buttons = tutorialPopup.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons) b.interactable = interactable;
        }
    }

    private void ApplyFonts()
    {
        TMP_FontAsset font = ResolveFont();
        if (font == null) return;
        if (bottomHint != null && bottomHint.font != font) bottomHint.font = font;
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
        fadeDuration = Mathf.Max(0.05f, fadeDuration);
        hintPulseDuration = Mathf.Max(0.1f, hintPulseDuration);
        ApplyFonts();
    }
#endif
}
