using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerStatController))]
public class PlayerMoveController : MonoBehaviour
{
    private float currentMoveSpeed;
    private Rigidbody2D playerRigidbody;
    private PlayerStatController playerStatController;
    private InputAction moveAction;

    public Vector2 MovementInput { get; private set; }

    public bool CanMove { get; set; } = true;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
        playerStatController = GetComponent<PlayerStatController>();
        moveAction = GetComponent<PlayerInput>().actions.FindAction("Move", true);
    }

    private void Start()
    {
        currentMoveSpeed = playerStatController.DefaultMoveSpeed;
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
