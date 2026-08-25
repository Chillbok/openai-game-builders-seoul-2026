using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임오버 오케스트레이터. PlayerStatController.Died를 단일 진입점으로
/// Die 애니 + 카메라 줌 + 반투명 오버레이 + 점수 동결 + 적 스폰 중단 + GameOver UI를 처리한다.
/// WebGL 호환: 모든 보간은 unscaledDeltaTime 기반.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameOverController : MonoBehaviour
{
    private static GameOverController instance;
    private static bool isGameOverStatic;

    public static GameOverController Instance => instance;
    public static bool IsGameOverStatic => isGameOverStatic;
    public bool IsGameOver => isGameOverStatic;

    [Header("참조")]
    [SerializeField]
    private PlayerStatController playerStatController;
    [SerializeField]
    private PlayerAnimationController playerAnimationController;
    [SerializeField]
    private PlayerMoveController playerMoveController;
    [SerializeField]
    private EnemyWaveSpawner waveSpawner;
    [SerializeField]
    private MapGenerator mapGenerator;
    [SerializeField]
    private DoorController doorController;
    [SerializeField]
    private ScoreController scoreController;
    [SerializeField]
    private Camera targetCamera;

    [Header("폰트 (중앙 관리)")]
    [Tooltip("비어 있으면 GameFontConfig 또는 TMP Settings 기본값을 사용한다")]
    [SerializeField]
    private GameFontConfig fontConfig;

    [Header("연출")]
    [SerializeField, Min(0f)]
    private float cameraZoomMultiplier = 0.7f;
    [SerializeField, Min(0f)]
    private float cameraZoomDuration = 0.3f;
    [SerializeField, Min(0f)]
    private float overlayFadeDuration = 0.3f;
    [SerializeField, Min(0f)]
    private float panelFadeDuration = 0.2f;
    [SerializeField, Range(0f, 1f)]
    private float overlayTargetAlpha = 0.5f;

    private CanvasGroup overlayGroup;
    private Image overlayImage;
    private GameObject overlayRoot;
    private CanvasGroup panelGroup;
    private readonly List<GameObject> managedUIObjects = new List<GameObject>();

    // GameOver UI refs
    private TMP_Text totalScoreText;
    private TMP_Text killScoreText;
    private TMP_Text executionScoreText;
    private TMP_Text killCountText;
    private TMP_Text executionCountText;
    private TMP_Text highScoreText;
    private TMP_Text newRecordBadge;
    private Button restartButton;

    private Vector3 cameraStartPos;
    private Quaternion cameraStartRot;
    private float cameraStartSize;
    private bool cameraStateStored;
    private bool hasTriggeredGameOver;
    private Coroutine gameOverRoutine;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        isGameOverStatic = false;
        hasTriggeredGameOver = false;
        CacheReferences();
        CreateOverlay();
        CreatePanel();
        SetOverlayAlpha(0f);
        SetPanelAlpha(0f);
        SetPanelInteractable(false);
    }

    private void OnEnable()
    {
        if (playerStatController != null)
        {
            playerStatController.Died += HandlePlayerDied;
        }
    }

    private void OnDisable()
    {
        if (playerStatController != null)
        {
            playerStatController.Died -= HandlePlayerDied;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            isGameOverStatic = false;
        }
    }

    private void CacheReferences()
    {
        if (playerStatController == null) playerStatController = FindFirstObjectByType<PlayerStatController>();
        if (playerAnimationController == null && playerStatController != null) playerAnimationController = playerStatController.GetComponent<PlayerAnimationController>();
        if (playerAnimationController == null) playerAnimationController = FindFirstObjectByType<PlayerAnimationController>();
        if (playerMoveController == null && playerStatController != null) playerMoveController = playerStatController.GetComponent<PlayerMoveController>();
        if (playerMoveController == null) playerMoveController = FindFirstObjectByType<PlayerMoveController>();
        if (waveSpawner == null) waveSpawner = FindFirstObjectByType<EnemyWaveSpawner>();
        if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();
        if (doorController == null) doorController = FindFirstObjectByType<DoorController>();
        if (scoreController == null) scoreController = FindFirstObjectByType<ScoreController>();
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) targetCamera = FindFirstObjectByType<Camera>();
    }

    private void CreateOverlay()
    {
        overlayRoot = new GameObject("GameOverOverlay");
        overlayRoot.transform.SetParent(transform, false);
        managedUIObjects.Add(overlayRoot);

        Canvas canvas = overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = overlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        overlayRoot.AddComponent<GraphicRaycaster>();

        overlayGroup = overlayRoot.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        GameObject imgObj = new GameObject("OverlayImage");
        imgObj.transform.SetParent(overlayRoot.transform, false);
        overlayImage = imgObj.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 1f);
        overlayImage.raycastTarget = false;
        RectTransform rt = overlayImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        overlayRoot.SetActive(true);
        imgObj.SetActive(true);
    }

    private void CreatePanel()
    {
        GameObject panelRoot = new GameObject("GameOverPanel");
        panelRoot.transform.SetParent(transform, false);
        managedUIObjects.Add(panelRoot);

        Canvas canvas = panelRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;

        CanvasScaler scaler = panelRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        panelRoot.AddComponent<GraphicRaycaster>();

        panelGroup = panelRoot.AddComponent<CanvasGroup>();
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        // Background dim
        GameObject bgObj = new GameObject("PanelBackground");
        bgObj.transform.SetParent(panelRoot.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);
        RectTransform bgRt = bg.rectTransform;
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = new Vector2(520f, 560f);
        bgRt.anchoredPosition = Vector2.zero;

        // Vertical layout container
        GameObject content = new GameObject("Content");
        content.transform.SetParent(panelRoot.transform, false);
        RectTransform contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.5f, 0.5f);
        contentRt.anchorMax = new Vector2(0.5f, 0.5f);
        contentRt.sizeDelta = new Vector2(480f, 520f);
        contentRt.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title
        CreateText(content.transform, "GAME OVER", 36, FontStyles.Bold, new Color(0.95f, 0.2f, 0.2f, 1f));

        totalScoreText = CreateText(content.transform, "총 점수: 0", 28, FontStyles.Bold, Color.white);
        killScoreText = CreateText(content.transform, "처치 점수: 0", 20, FontStyles.Normal, new Color(0.9f, 0.9f, 0.9f, 1f));
        executionScoreText = CreateText(content.transform, "처형 점수: 0", 20, FontStyles.Normal, new Color(0.9f, 0.9f, 0.9f, 1f));
        killCountText = CreateText(content.transform, "처치 수: 0", 20, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f, 1f));
        executionCountText = CreateText(content.transform, "처형 수: 0", 20, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f, 1f));
        highScoreText = CreateText(content.transform, "최고 점수: 0", 18, FontStyles.Italic, new Color(1f, 0.84f, 0f, 1f));
        newRecordBadge = CreateText(content.transform, "NEW RECORD!", 22, FontStyles.Bold, new Color(1f, 0.84f, 0f, 1f));
        newRecordBadge.gameObject.SetActive(false);

        // Button
        GameObject btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(content.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(200f, 50f);
        restartButton = btnObj.AddComponent<Button>();
        restartButton.targetGraphic = btnImg;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TMP_Text btnText = txtObj.AddComponent<TextMeshProUGUI>();
        btnText.font = ResolveFont();
        btnText.text = "다시 시작";
        btnText.fontSize = 22;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        restartButton.onClick.AddListener(RequestRestart);

        panelRoot.SetActive(true);
    }

    private TMP_FontAsset ResolveFont()
    {
        if (fontConfig != null && fontConfig.DefaultFont != null) return fontConfig.DefaultFont;
        // TMP Settings가 GameFontConfigSync로 이미 DungGeunMo로 교체된 상태
        return TMP_Settings.defaultFontAsset;
    }

    private TMP_Text CreateText(Transform parent, string text, float size, FontStyles style, Color color)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = ResolveFont();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = size + 8f;
        return tmp;
    }

    private void HandlePlayerDied()
    {
        if (hasTriggeredGameOver) return;
        hasTriggeredGameOver = true;
        isGameOverStatic = true;

        CacheReferences();

        // 즉시 입력 차단 + 물리 정지
        if (playerMoveController != null)
        {
            playerMoveController.CanMove = false;
            Rigidbody2D rb = playerMoveController.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        // 처형 연출 취소
        var exec = playerStatController != null ? playerStatController.GetComponent<PlayerExecutionController>() : null;
        if (exec == null) exec = FindFirstObjectByType<PlayerExecutionController>();
        if (exec != null && exec.IsBusy)
        {
            // CancelExecution은 Died 핸들러에서도 호출되지만 명시적으로 한번 더
            exec.SendMessage("CancelExecution", SendMessageOptions.DontRequireReceiver);
        }

        // 회피 중단
        var dodge = playerStatController != null ? playerStatController.GetComponent<PlayerDodge>() : null;
        if (dodge == null) dodge = FindFirstObjectByType<PlayerDodge>();
        if (dodge != null && dodge.IsDodging)
        {
            dodge.SendMessage("CancelForExecution", SendMessageOptions.DontRequireReceiver);
        }

        // 공격 히트박스 비활성
        var hitbox = playerStatController != null ? playerStatController.GetComponent<PlayerAttackHitboxController>() : null;
        if (hitbox != null) hitbox.DisableAllHitboxes();

        // Die 애니
        if (playerAnimationController != null)
        {
            playerAnimationController.PlayDie();
        }

        // 점수/스폰 동결
        if (scoreController != null) scoreController.Freeze();
        if (waveSpawner != null) waveSpawner.StopSpawning();

        // 적 Freeze
        FreezeAllEnemies();

        // 카메라 상태 저장
        StoreCameraState();

        // 연출 시작
        if (gameOverRoutine != null) StopCoroutine(gameOverRoutine);
        gameOverRoutine = StartCoroutine(GameOverSequence());
    }

    private void FreezeAllEnemies()
    {
        var enemies = FindObjectsByType<EnemyStateMachine>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e == null) continue;
            e.enabled = false;
            var rb = e.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }

    private void UnfreezeAllEnemies()
    {
        var enemies = FindObjectsByType<EnemyStateMachine>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e == null) continue;
            e.enabled = true;
        }
    }

    private void StoreCameraState()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;
        cameraStartPos = targetCamera.transform.position;
        cameraStartRot = targetCamera.transform.rotation;
        cameraStartSize = targetCamera.orthographicSize;
        cameraStateStored = true;
    }

    private void RestoreCameraState()
    {
        if (!cameraStateStored || targetCamera == null) return;
        targetCamera.transform.position = cameraStartPos;
        targetCamera.transform.rotation = cameraStartRot;
        targetCamera.orthographicSize = cameraStartSize;
        cameraStateStored = false;
    }

    private IEnumerator GameOverSequence()
    {
        // 오버레이 + 카메라 줌 병렬 (unscaledDeltaTime)
        float elapsed = 0f;
        float zoomDur = Mathf.Max(0.05f, cameraZoomDuration);
        float overlayDur = Mathf.Max(0.05f, overlayFadeDuration);
        float maxDur = Mathf.Max(zoomDur, overlayDur);

        Vector3 targetCamPos = cameraStartPos;
        float targetSize = cameraStartSize * cameraZoomMultiplier;
        if (playerStatController != null)
        {
            targetCamPos = playerStatController.transform.position;
            targetCamPos.z = cameraStartPos.z;
        }

        while (elapsed < maxDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float tZoom = Mathf.Clamp01(elapsed / zoomDur);
            float tOverlay = Mathf.Clamp01(elapsed / overlayDur);

            if (targetCamera != null && cameraStateStored)
            {
                targetCamera.transform.position = Vector3.Lerp(cameraStartPos, targetCamPos, tZoom);
                targetCamera.orthographicSize = Mathf.Lerp(cameraStartSize, targetSize, tZoom);
            }

            SetOverlayAlpha(Mathf.Lerp(0f, overlayTargetAlpha, tOverlay));
            yield return null;
        }

        if (targetCamera != null && cameraStateStored)
        {
            targetCamera.transform.position = targetCamPos;
            targetCamera.orthographicSize = targetSize;
        }
        SetOverlayAlpha(overlayTargetAlpha);

        // 스냅샷으로 UI 갱신
        UpdatePanelTexts();

        // 패널 페이드인
        float pElapsed = 0f;
        float pDur = Mathf.Max(0.05f, panelFadeDuration);
        SetPanelInteractable(false);
        while (pElapsed < pDur)
        {
            pElapsed += Time.unscaledDeltaTime;
            SetPanelAlpha(Mathf.Clamp01(pElapsed / pDur));
            yield return null;
        }
        SetPanelAlpha(1f);
        SetPanelInteractable(true);
    }

    private void UpdatePanelTexts()
    {
        if (scoreController == null) scoreController = FindFirstObjectByType<ScoreController>();
        if (scoreController == null) return;

        bool isNewRecord = scoreController.TryCommitHighScore();
        ScoreSnapshot snap = scoreController.GetSnapshot();

        if (totalScoreText != null) totalScoreText.text = $"총 점수: {snap.TotalScore}";
        if (killScoreText != null) killScoreText.text = $"처치 점수: {snap.KillScore}";
        if (executionScoreText != null) executionScoreText.text = $"처형 점수: {snap.ExecutionScore}";
        if (killCountText != null) killCountText.text = $"처치 수: {snap.KillCount}";
        if (executionCountText != null) executionCountText.text = $"처형 수: {snap.ExecutionCount}";
        if (highScoreText != null) highScoreText.text = $"최고 점수: {snap.HighScore}";
        if (newRecordBadge != null) newRecordBadge.gameObject.SetActive(isNewRecord);
    }

    private void SetOverlayAlpha(float a)
    {
        if (overlayGroup != null)
        {
            overlayGroup.alpha = a;
            overlayGroup.blocksRaycasts = a > 0.01f;
        }
        if (overlayImage != null)
        {
            Color c = overlayImage.color;
            c.a = a > 0.001f ? 1f : 0f;
            // 실제 알파는 CanvasGroup에서 제어, Image 자체는 항상 불투명으로 두고 그룹 알파로 페이드
            overlayImage.enabled = a > 0.001f;
        }
    }

    private void SetPanelAlpha(float a)
    {
        if (panelGroup != null)
        {
            panelGroup.alpha = a;
        }
    }

    private void SetPanelInteractable(bool v)
    {
        if (panelGroup != null)
        {
            panelGroup.interactable = v;
            panelGroup.blocksRaycasts = v;
        }
    }

    public void RequestRestart()
    {
        if (!isGameOverStatic) return;
        StartCoroutine(RestartSequence());
    }

    private IEnumerator RestartSequence()
    {
        SetPanelInteractable(false);

        // 패널/오버레이 페이드아웃 (unscaled)
        float elapsed = 0f;
        float dur = 0.2f;
        float startOverlay = overlayGroup != null ? overlayGroup.alpha : overlayTargetAlpha;
        float startPanel = panelGroup != null ? panelGroup.alpha : 1f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            SetOverlayAlpha(Mathf.Lerp(startOverlay, 0f, t));
            SetPanelAlpha(Mathf.Lerp(startPanel, 0f, t));
            yield return null;
        }
        SetOverlayAlpha(0f);
        SetPanelAlpha(0f);

        // 1. 모든 적 삭제
        DeleteAllEnemies();

        yield return null;

        // 2. 맵 재생성 (mapIndex=0)
        if (mapGenerator != null)
        {
            mapGenerator.ResetForRestart();
        }

        yield return null;

        // 3. 플레이어 리셋
        if (playerStatController != null)
        {
            playerStatController.ResetRuntimeStats();
            // 물리 리셋
            Rigidbody2D rb = playerStatController.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            // 중앙 재배치
            if (mapGenerator != null && mapGenerator.WallsTilemap != null && mapGenerator.CurrentLayout != null)
            {
                Vector2Int center = mapGenerator.CurrentLayout.GetCenter();
                Vector3 worldCenter = mapGenerator.WallsTilemap.GetCellCenterWorld(new Vector3Int(center.x, center.y, 0));
                worldCenter.z = playerStatController.transform.position.z;
                playerStatController.transform.position = worldCenter;
            }
        }

        // 4. 카메라 복구
        RestoreCameraState();
        var camFollow = targetCamera != null ? targetCamera.GetComponent<CameraFollowController>() : null;
        if (camFollow == null) camFollow = FindFirstObjectByType<CameraFollowController>();
        if (camFollow != null) camFollow.SnapToTarget();

        // 5. 문 재배치 (반드시 방 내부 빈 셀)
        if (doorController != null)
        {
            // Map 재생성 직후 SpawnAreaProvider Bake 완료 보장 후 배치
            yield return null;
            doorController.TryPlaceDoorRandomly();
            doorController.ForceLockedVisual();
        }

        // 7. 점수 리셋 (HighScore 보존) — 웨이브 재시작 전 동결 해제
        if (scoreController != null)
        {
            scoreController.ResetForNewGame();
        }

        // 8. 상태 리셋 — 웨이브 재시작 전 GameOver 해제 (Spawn 가드 통과)
        isGameOverStatic = false;
        hasTriggeredGameOver = false;
        if (gameOverRoutine != null) gameOverRoutine = null;

        // 6. 웨이브 재시작 (상태 리셋 이후)
        if (waveSpawner != null)
        {
            waveSpawner.ClearTracking();
            // Bake 이후이므로 즉시 시작 가능
            waveSpawner.TryBeginSurvivalWave();
        }

        // 9. 플레이어 이동 재허용
        if (playerMoveController != null && playerStatController != null && !playerStatController.IsDead)
        {
            playerMoveController.CanMove = true;
        }
        else if (playerMoveController != null)
        {
            playerMoveController.CanMove = true;
        }

        // 10. 적 Unfreeze (이미 삭제 후 새로 스폰된 적은 enabled 상태)
        UnfreezeAllEnemies();
    }

    private void DeleteAllEnemies()
    {
        // WaveSpawner 트래킹 클리어
        if (waveSpawner != null) waveSpawner.ClearTracking();

        var enemies = FindObjectsByType<EnemyStateMachine>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e == null) continue;
            Destroy(e.gameObject);
        }

        // 추가로 EnemyStatController만 있는 오브젝트도 제거
        var stats = FindObjectsByType<EnemyStatController>(FindObjectsSortMode.None);
        foreach (var s in stats)
        {
            if (s == null) continue;
            // 이미 위에서 파괴된 경우 스킵
            if (s.GetComponent<EnemyStateMachine>() != null) continue;
            Destroy(s.gameObject);
        }
    }
}
