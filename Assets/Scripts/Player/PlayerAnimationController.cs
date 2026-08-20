using UnityEngine;

[RequireComponent(typeof(Animator), typeof(PlayerMoveController))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("패러미터 이름들")]
    [Header("이동 관련")]
    [SerializeField]
    [Tooltip("플레이어가 이동 중인지 확인하기 위한 패러미터 이름")]
    private string animatorMoveParameterName;

    [Header("공격 관련")]
    [SerializeField]
    [Tooltip("플레이어의 공격 트리거 패러미터 이름")]
    // 이 변수는 트리거 패러미터임
    private string attackTriggerParameterName;
    
    [SerializeField]
    [Tooltip("공격 카운트를 위한 패러미터 이름")]
    // 이 변수는 int 패러미터임
    private string attackCountParameterName;

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
        ChangePlayerMoveAnimationPerFrame();
    }
    
    void ChangePlayerMoveAnimationPerFrame()
    {
        bool isMoving = playerMoveController.MovementInput.sqrMagnitude > 0.0001f;
        animator.SetBool(animatorMoveParameterHash, isMoving);
    }
}
