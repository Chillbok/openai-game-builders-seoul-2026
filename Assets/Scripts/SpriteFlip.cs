using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer), typeof(PlayerMoveController))]
public class SpriteFlip : MonoBehaviour
{
    [Header("애니메이터")]
    [SerializeField]
    private Animator animator;

    private SpriteRenderer spriteRenderer;
    private PlayerMoveController playerMoveController;

    [Header("애니메이션 패러미터 이름")]
    [SerializeField]
    private string rightAnimatorParameterName;

    private bool isFacingRight;
    private bool? flipXOverride;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerMoveController = GetComponent<PlayerMoveController>();

        isFacingRight = true;
        FlipSpriteToMoveDirection(isFacingRight);
    }

    private void LateUpdate()
    {
        DefineSpriteDirection();
    }

    private void DefineSpriteDirection()
    {
        if (flipXOverride.HasValue)
        {
            isFacingRight = !flipXOverride.Value;
            spriteRenderer.flipX = flipXOverride.Value;
            animator.SetBool(rightAnimatorParameterName, isFacingRight);
            return;
        }

        float horizontalInput = playerMoveController.MovementInput.x;

        if (horizontalInput > 0f)
        {
            isFacingRight = true;
        }
        else if (horizontalInput < 0f)
        {
            isFacingRight = false;
        }

        FlipSpriteToMoveDirection(isFacingRight);
    }

    public void SetFlipXOverride(bool flipX)
    {
        flipXOverride = flipX;
        isFacingRight = !flipX;
    }

    public void ClearFlipXOverride()
    {
        flipXOverride = null;
    }

    private void FlipSpriteToMoveDirection(bool isFacingRight)
    {
        spriteRenderer.flipX = !isFacingRight;
        animator.SetBool(rightAnimatorParameterName, isFacingRight);
    }
}
