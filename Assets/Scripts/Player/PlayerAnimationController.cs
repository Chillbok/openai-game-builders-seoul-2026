using UnityEngine;

[RequireComponent(typeof(Animator), typeof(PlayerMoveController))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("패러미터 이름들")]
    [SerializeField]
    [Tooltip("플레이어가 이동 중인지 확인하기 위한 패러미터 이름")]
    private string animatorMoveParameterName;

    private Animator animator;
    private PlayerMoveController playerMoveController;
    private int animatorMoveParameterHash;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerMoveController = GetComponent<PlayerMoveController>();
        animatorMoveParameterHash = Animator.StringToHash(animatorMoveParameterName);
    }

    private void Update()
    {
        bool isMoving = playerMoveController.MovementInput.sqrMagnitude > 0.0001f;
        animator.SetBool(animatorMoveParameterHash, isMoving);
    }
}
