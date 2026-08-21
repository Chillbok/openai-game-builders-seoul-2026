using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TempGivePlayerDamage : MonoBehaviour
{
    [SerializeField]
    [Tooltip("플레이어에게 줄 데미지")]
    [Min(0f)]
    private float damage;

	void OnEnable()
	{
		Collider2D collider = GetComponent<Collider2D>();
		if (!collider.isTrigger) collider.isTrigger = true;
	}

	void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.layer != LayerMask.NameToLayer("Player")) return;

		PlayerStatController playerStatController = collision.GetComponentInParent<PlayerStatController>();
		if (playerStatController == null) return;

		playerStatController.TryTakeDamage(damage);
	}
}
