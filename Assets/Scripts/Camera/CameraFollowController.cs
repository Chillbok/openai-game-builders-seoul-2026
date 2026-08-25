using UnityEngine;

/// <summary>
/// 2D 카메라가 플레이어를 부드럽게 따라가도록 하는 컴포넌트.
/// Main Camera에 부착하여 사용한다. LateUpdate에서 타겟 위치로 보간한다.
/// PlayerExecutionController의 처형 연출(IsPresenting) 중에는 추적을 일시 정지하여
/// 연출 카메라 제어를 방해하지 않는다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CameraFollowController : MonoBehaviour
{
    private const float DefaultSmoothTime = 0.15f;
    private const float DefaultFollowSpeed = 5f;

    [Header("대상")]
    [SerializeField]
    [Tooltip("따라갈 대상 Transform. 비워 두면 Player 태그로 자동 탐색한다.")]
    private Transform target;

    [SerializeField]
    [Tooltip("타겟으로부터의 오프셋. Z는 카메라 깊이 유지에 사용된다 (기본 0,0,-10)")]
    private Vector3 offset = new Vector3(0f, 0f, -10f);

    [SerializeField]
    [Tooltip("true면 시작 시 카메라와 타겟의 현재 차이로 오프셋을 자동 계산한다.")]
    private bool autoOffset = false;

    [Header("보간")]
    [SerializeField, Min(0f)]
    [Tooltip("추적 부드러움. 0이면 즉시 추적, 값이 클수록 부드럽게 따라간다.")]
    private float smoothTime = DefaultSmoothTime;

    [SerializeField, Min(0f)]
    [Tooltip("Lerp 기반 추적 속도. smoothTime이 0일 때만 사용된다.")]
    private float followSpeed = DefaultFollowSpeed;

    [SerializeField]
    [Tooltip("true면 Vector3.SmoothDamp를 사용하고, false면 Lerp를 사용한다.")]
    private bool useSmoothDamp = true;

    [Header("클램프")]
    [SerializeField]
    [Tooltip("true면 맵 경계 밖으로 카메라가 나가지 않도록 클램프한다.")]
    private bool clampToMapBounds = true;

    [SerializeField]
    [Tooltip("맵 경계 계산에 사용할 MapGenerator. 비워 두면 씬에서 자동 탐색한다.")]
    private MapGenerator mapGenerator;

    [Header("연출 호환")]
    [SerializeField]
    [Tooltip("true면 PlayerExecutionController가 처형 연출 중일 때 추적을 일시 정지한다.")]
    private bool pauseDuringExecution = true;

    [SerializeField]
    [Tooltip("true면 GameOverController가 게임오버 연출 중일 때 추적을 일시 정지한다.")]
    private bool pauseDuringGameOver = true;

    private Camera cachedCamera;
    private Vector3 velocity;
    private float fixedZ;
    private PlayerExecutionController targetExecutionController;
    private bool offsetInitialized;

    public Transform Target => target;
    public Vector3 Offset => offset;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        fixedZ = transform.position.z;

        // offset.z가 0이면 fixedZ로 보정 (씬 초기값  -10 유지)
        if (Mathf.Approximately(offset.z, 0f) && !Mathf.Approximately(fixedZ, 0f))
        {
            offset.z = fixedZ;
        }

        if (target == null)
        {
            TryFindTarget();
        }

        CacheExecutionController();

        if (mapGenerator == null && clampToMapBounds)
        {
            mapGenerator = FindAnyObjectByType<MapGenerator>();
        }

        if (autoOffset && target != null)
        {
            offset = transform.position - target.position;
            offset.z = fixedZ - target.position.z;
            offsetInitialized = true;
        }
    }

    private void Start()
    {
        if (target == null)
        {
            TryFindTarget();
            CacheExecutionController();
        }

        // 시작 시 즉시 타겟 위치로 스냅하여 초기 프레임 튐 방지
        if (target != null)
        {
            Vector3 desired = GetDesiredPosition();
            desired = ClampToBoundsIfNeeded(desired);
            transform.position = desired;
            velocity = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            TryFindTarget();
            if (target == null) return;
            CacheExecutionController();
        }

        if (pauseDuringExecution && targetExecutionController != null && targetExecutionController.IsPresenting)
        {
            return;
        }

        if (pauseDuringGameOver && GameOverController.IsGameOverStatic)
        {
            return;
        }

        Vector3 desiredPosition = GetDesiredPosition();
        desiredPosition = ClampToBoundsIfNeeded(desiredPosition);

        if (smoothTime <= 0f)
        {
            if (followSpeed <= 0f)
            {
                transform.position = desiredPosition;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);
                // Lerp가 목표에 매우 가까워지면 스냅
                if ((transform.position - desiredPosition).sqrMagnitude < 0.0001f)
                {
                    transform.position = desiredPosition;
                }
            }
        }
        else
        {
            if (useSmoothDamp)
            {
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
            }
            else
            {
                float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime / Mathf.Max(0.01f, smoothTime));
                transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
            }
        }
    }

    private Vector3 GetDesiredPosition()
    {
        if (!offsetInitialized && autoOffset && target != null)
        {
            // 타겟이 늦게 생성된 경우 한 번 더 계산
            offset = transform.position - target.position;
            offset.z = fixedZ - target.position.z;
            offsetInitialized = true;
        }

        Vector3 desired = target.position + offset;
        // Z는 고정값 유지 (2D orthographic 카메라)
        desired.z = fixedZ;
        if (Mathf.Approximately(offset.z, 0f) == false)
        {
            // offset.z가 의도된 값이면 반영하되, 항상 fixedZ 기준으로 동작
            // (target.position.z가 0이 아닌 경우 대비)
            desired.z = target.position.z + offset.z;
        }
        else
        {
            desired.z = fixedZ;
        }
        return desired;
    }

    private Vector3 ClampToBoundsIfNeeded(Vector3 desired)
    {
        if (!clampToMapBounds) return desired;
        if (cachedCamera == null) return desired;
        if (!cachedCamera.orthographic) return desired;

        MapGenerator generator = mapGenerator;
        if (generator == null)
        {
            generator = FindAnyObjectByType<MapGenerator>();
            mapGenerator = generator;
        }

        MapLayout layout = generator != null ? generator.CurrentLayout : null;
        if (layout == null) return desired;

        float halfHeight = cachedCamera.orthographicSize;
        float halfWidth = halfHeight * cachedCamera.aspect;

        // 맵은 (0,0) ~ (width, height) 영역에 타일이 배치된다. 셀 중심이 정수 좌표.
        // 월드 경계는 0 ~ width, 0 ~ height 로 간주한다.
        float minX = halfWidth;
        float maxX = layout.Width - halfWidth;
        float minY = halfHeight;
        float maxY = layout.Height - halfHeight;

        // 맵이 뷰포트보다 작으면 중앙에 고정
        if (maxX < minX)
        {
            desired.x = layout.Width * 0.5f;
        }
        else
        {
            desired.x = Mathf.Clamp(desired.x, minX, maxX);
        }

        if (maxY < minY)
        {
            desired.y = layout.Height * 0.5f;
        }
        else
        {
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
        }

        return desired;
    }

    private void TryFindTarget()
    {
        if (target != null) return;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            return;
        }

        // 태그 탐색 실패 시 PlayerMoveController로 폴백
        var moveController = FindAnyObjectByType<PlayerMoveController>();
        if (moveController != null)
        {
            target = moveController.transform;
        }
    }

    private void CacheExecutionController()
    {
        if (target == null)
        {
            targetExecutionController = null;
            return;
        }
        targetExecutionController = target.GetComponent<PlayerExecutionController>();
    }

    /// <summary>
    /// 외부에서 타겟을 변경할 때 사용한다.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        CacheExecutionController();
        velocity = Vector3.zero;
    }

    /// <summary>
    /// 현재 위치에서 타겟으로 즉시 스냅한다.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = ClampToBoundsIfNeeded(GetDesiredPosition());
        velocity = Vector3.zero;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (smoothTime < 0f) smoothTime = 0f;
        if (followSpeed < 0f) followSpeed = 0f;
        // 에디터에서 Z 오프셋이 0으로 잘못 설정되는 것을 방지
        if (cachedCamera == null) cachedCamera = GetComponent<Camera>();
    }
#endif
}
