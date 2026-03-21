using UnityEngine;

public class OrbProjectile : MonoBehaviour
{
    private PlayerOrbController owner;
    private EnemyController target;
    private Vector3 fallbackDirection;
    private float speed;
    private float lifetime;
    private float damage;
    private float fireDuration;
    private float fireDamagePerSecond;

    public void Initialize(
        PlayerOrbController projectileOwner,
        EnemyController projectileTarget,
        Vector3 direction,
        float projectileSpeed,
        float projectileLifetime,
        float projectileDamage,
        float burnDuration,
        float burnDamage)
    {
        owner = projectileOwner;
        target = projectileTarget;
        fallbackDirection = direction.normalized;
        speed = projectileSpeed;
        lifetime = projectileLifetime;
        damage = projectileDamage;
        fireDuration = burnDuration;
        fireDamagePerSecond = burnDamage;
    }

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        EnemyController currentTarget = target != null ? target : EnemyController.FindClosestAlive(transform.position, 6f);
        Vector3 moveDirection = fallbackDirection;

        if (currentTarget != null)
        {
            Vector3 targetPosition = currentTarget.transform.position + Vector3.up * 1f;
            Vector3 toTarget = targetPosition - transform.position;
            if (toTarget.sqrMagnitude <= 0.25f)
            {
                Impact(currentTarget);
                return;
            }

            moveDirection = toTarget.normalized;
            fallbackDirection = moveDirection;
        }

        transform.position += moveDirection * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (owner != null && other.GetComponentInParent<PlayerController>() != null)
        {
            return;
        }

        EnemyController enemy = other.GetComponent<EnemyController>();
        enemy ??= other.GetComponentInParent<EnemyController>();
        if (enemy == null)
        {
            return;
        }

        Impact(enemy);
    }

    void Impact(EnemyController enemy)
    {
        if (enemy == null)
        {
            Destroy(gameObject);
            return;
        }

        enemy.TakeDamage(damage, false);

        if (fireDuration > 0f && fireDamagePerSecond > 0f)
        {
            enemy.ApplyBurn(fireDuration, fireDamagePerSecond);
        }

        Destroy(gameObject);
    }
}
