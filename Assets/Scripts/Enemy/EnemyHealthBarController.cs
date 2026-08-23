using UnityEngine;
using UnityEngine.UI;

// 적 개체 아래에 표시되며 EnemyStatController의 체력 이벤트를 구독해
// World Space 체력바를 갱신한다. DemonEnemy.prefab: EnemyHealthBar 자식에 부착된다.
[DisallowMultipleComponent]
public sealed class EnemyHealthBarController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField]
    [Tooltip("체력 이벤트를 구독할 적 스탯 컨트롤러 (비워두면 부모에서 자동 탐색)")]
    private EnemyStatController enemyStatController;

    [SerializeField]
    [Tooltip("체력 비율만큼 채워지는 필 이미지 (Filled, Horizontal)")]
    private Image fillImage;

    [SerializeField]
    [Tooltip("체력바 전체를 켜고 끌 Canvas (사망 시 숨김 등에 사용)")]
    private Canvas healthBarCanvas;

    [Header("표시 옵션")]
    [SerializeField]
    [Tooltip("체력이 가득 찼을 때 체력바를 숨길지 여부")]
    private bool hideWhenFull = false;

    [SerializeField]
    [Tooltip("사망 시 체력바를 숨길지 여부")]
    private bool hideOnDeath = true;

    private bool subscribed;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        Subscribe();

        // 초기 체력 반영 (Awake 시점에 이미 초기화됨)
        if (enemyStatController != null && enemyStatController.IsInitialized)
        {
            UpdateFill(enemyStatController.CurrentHP);
            UpdateVisibility(enemyStatController.CurrentHP);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void CacheReferences()
    {
        if (enemyStatController == null)
        {
            enemyStatController = GetComponentInParent<EnemyStatController>();
        }

        if (fillImage == null)
        {
            fillImage = GetComponentInChildren<Image>(true);
            // Background와 Fill이 둘 다 Image이므로, Fill을 우선 찾되 이름으로 구분
            if (fillImage != null && fillImage.name == "Background")
            {
                Image[] images = GetComponentsInChildren<Image>(true);
                foreach (Image img in images)
                {
                    if (img.name == "Fill")
                    {
                        fillImage = img;
                        break;
                    }
                }
            }
        }

        if (healthBarCanvas == null)
        {
            healthBarCanvas = GetComponent<Canvas>();
            if (healthBarCanvas == null)
            {
                healthBarCanvas = GetComponentInParent<Canvas>();
            }
        }
    }

    private void Subscribe()
    {
        if (subscribed || enemyStatController == null)
        {
            return;
        }

        enemyStatController.CurrentHPChanged += OnCurrentHPChanged;
        enemyStatController.Died += OnDied;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || enemyStatController == null)
        {
            return;
        }

        enemyStatController.CurrentHPChanged -= OnCurrentHPChanged;
        enemyStatController.Died -= OnDied;
        subscribed = false;
    }

    private void OnCurrentHPChanged(float currentHP)
    {
        UpdateFill(currentHP);
        UpdateVisibility(currentHP);
    }

    private void OnDied()
    {
        if (hideOnDeath)
        {
            SetCanvasVisible(false);
        }
        else
        {
            UpdateFill(0f);
        }
    }

    // PlayerScreenHUDController:88 와 동일한 fillAmount 계산을 재사용한다.
    private void UpdateFill(float currentHP)
    {
        if (fillImage == null || enemyStatController == null)
        {
            return;
        }

        float maxHP = enemyStatController.MaxHP;
        fillImage.fillAmount = maxHP > 0f ? Mathf.Clamp01(currentHP / maxHP) : 0f;
    }

    private void UpdateVisibility(float currentHP)
    {
        if (healthBarCanvas == null || enemyStatController == null)
        {
            return;
        }

        if (hideWhenFull)
        {
            float maxHP = enemyStatController.MaxHP;
            bool isFull = maxHP > 0f && Mathf.Approximately(currentHP, maxHP);
            // 풀피일 때는 숨기고, 아니면 표시 (사망 처리와 충돌하지 않게 풀피만 담당)
            if (isFull && currentHP > 0f)
            {
                SetCanvasVisible(false);
                return;
            }
        }

        // 사망이 아니고 풀피 숨김 조건에 걸리지 않았다면 표시
        if (enemyStatController.IsDead && hideOnDeath)
        {
            SetCanvasVisible(false);
        }
        else
        {
            SetCanvasVisible(true);
        }
    }

    private void SetCanvasVisible(bool visible)
    {
        if (healthBarCanvas != null)
        {
            healthBarCanvas.enabled = visible;
        }
        else
        {
            // Canvas가 없으면 루트 오브젝트 활성으로 대체
            gameObject.SetActive(visible);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (fillImage != null)
        {
            // 체력바 필은 좌->우로 채워지도록 강제 (Horizontal Filled)
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
        }
    }
#endif
}
