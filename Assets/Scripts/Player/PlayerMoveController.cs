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
    private Collider2D bodyCollider;
    private NoPushCollisionMover2D collisionMover;

    public Vector2 MovementInput { get; private set; }

    public bool CanMove { get; set; } = true;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
        playerStatController = GetComponent<PlayerStatController>();
        moveAction = GetComponent<PlayerInput>().actions.FindAction("Move", true);
        bodyCollider = GetComponent<Collider2D>();

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        int blockingLayerMask = 0;
        if (enemyLayer >= 0) blockingLayerMask |= 1 << enemyLayer;
        if (obstacleLayer >= 0) blockingLayerMask |= 1 << obstacleLayer;

        NoPushCollisionMover2D.ConfigureNoPushContact(bodyCollider, enemyLayer);
        collisionMover = new NoPushCollisionMover2D(
            playerRigidbody,
            bodyCollider,
            transform,
            blockingLayerMask);
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

        Vector2 delta = MovementInput * currentMoveSpeed * Time.fixedDeltaTime;
        TryMove(delta);
    }

    private void TryMove(Vector2 delta)
    {
        collisionMover?.Move(delta);
    }
}
