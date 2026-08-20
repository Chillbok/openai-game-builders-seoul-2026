using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D), typeof(PlayerInput))]
public class PlayerMoveController : MonoBehaviour
{
    [Header("플레이어 데이터")]
    [SerializeField]
    private PlayerData playerData;

    private float currentMoveSpeed;
    private Rigidbody2D playerRigidbody;
    private InputAction moveAction;

    public Vector2 MovementInput { get; private set; }

    public bool CanMove { get; set; } = true;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
        moveAction = GetComponent<PlayerInput>().actions.FindAction("Move", true);
    }

    private void Start()
    {
        currentMoveSpeed = playerData.DefaultMoveSpeed;
    }

    private void FixedUpdate()
    {
        MovementInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);

        if (!CanMove)
        {
            return;
        }

        playerRigidbody.MovePosition(
            playerRigidbody.position + MovementInput * currentMoveSpeed * Time.fixedDeltaTime);
    }
}
