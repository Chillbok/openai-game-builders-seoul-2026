using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 페이드 오버레이. Door 전이 시 페이드 아웃/인에 사용한다.
/// CanvasGroup 알파를 unscaledDeltaTime으로 제어해 WebGL 포커스 복귀와 무관하게 동작한다.
/// </summary>
public sealed class ScreenFadeController : MonoBehaviour
{
    private static ScreenFadeController instance;

    [SerializeField]
    private Image fadeImage;

    [SerializeField]
    private float fadeDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private Canvas canvas;

    public static ScreenFadeController Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureCanvas();
        SetAlpha(0f);
        if (fadeImage != null) fadeImage.raycastTarget = false;
    }

    private void EnsureCanvas()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (fadeImage == null)
        {
            GameObject imgObj = new GameObject("FadeImage");
            imgObj.transform.SetParent(transform, false);
            fadeImage = imgObj.AddComponent<Image>();
            fadeImage.color = Color.black;
            RectTransform rt = fadeImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        fadeImage.color = Color.black;
    }

    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null) canvasGroup.alpha = alpha;
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
            fadeImage.enabled = alpha > 0.001f;
        }
    }

    public System.Collections.IEnumerator FadeOut(System.Action onMid = null)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeDuration);
            SetAlpha(a);
            yield return null;
        }
        SetAlpha(1f);
        onMid?.Invoke();
    }

    public System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(t / fadeDuration);
            SetAlpha(a);
            yield return null;
        }
        SetAlpha(0f);
    }

    public void FadeOutIn(System.Action onMid)
    {
        StartCoroutine(FadeOutInRoutine(onMid));
    }

    private System.Collections.IEnumerator FadeOutInRoutine(System.Action onMid)
    {
        yield return FadeOut(onMid);
        yield return new WaitForSecondsRealtime(0.05f);
        yield return FadeIn();
    }

    /// <summary>
    /// StartMenu 전용: 페이드 아웃 → 비동기 씬 로드 대기 → 페이드 인.
    /// StartMenuController가 파괴되어도 페이드가 유지되도록 ScreenFadeController에서 실행한다.
    /// </summary>
    public void FadeOutLoadAndIn(string sceneName, System.Action<bool> onComplete = null)
    {
        StartCoroutine(FadeOutLoadAndInRoutine(sceneName, onComplete));
    }

    private System.Collections.IEnumerator FadeOutLoadAndInRoutine(string sceneName, System.Action<bool> onComplete)
    {
        // 페이드 아웃 (검은 화면)
        yield return FadeOut(null);
        // 씬 로드
        bool success = false;
        UnityEngine.AsyncOperation op = null;
        try
        {
            op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"ScreenFadeController: LoadSceneAsync({sceneName}) 반환 null", this);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ScreenFadeController: LoadSceneAsync({sceneName}) 예외 — {e.Message}", this);
            op = null;
        }

        if (op != null)
        {
            op.allowSceneActivation = true;
            while (!op.isDone)
            {
                yield return null;
            }
            success = true;
        }

        yield return new WaitForSecondsRealtime(0.05f);
        yield return FadeIn();
        onComplete?.Invoke(success);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        fadeDuration = Mathf.Max(0.05f, fadeDuration);
    }
#endif
}
