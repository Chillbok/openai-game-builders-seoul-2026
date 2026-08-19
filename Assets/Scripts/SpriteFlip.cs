using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class SpriteFlip : MonoBehaviour
{
    [Header("애니메이터")]
    [SerializeField]
    private Animator animator;

    private SpriteRenderer spriteRenderer;

    [Header("애니메이션 패러미터 이름")]
    [SerializeField]
    private string rightAnimatorParameterName;

    private bool isFacingRight;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        DefineSpriteDirection();
    }

    private void DefineSpriteDirection()
    {
        isFacingRight = animator.GetBool(rightAnimatorParameterName);
        spriteRenderer.flipX = !isFacingRight;
    }
}
