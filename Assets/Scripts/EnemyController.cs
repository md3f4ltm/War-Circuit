using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    public float health = 30f;
    public float attackRange = 2.0f;
    public float attackCooldown = 1.6f;
    public float attackDamage = 5f;
    public float rotationSpeed = 8f;
    private const string IdleAnim = "Idle_Battle_SwordAndShiled";
    private const string MoveAnim = "MoveFWD_Normal_InPlace_SwordAndShield";
    private const string AttackAnim = "Attack01_SwordAndShiled";
    private const string HitAnim = "GetHit01_SwordAndShield";
    private const string DieAnim = "Die01_SwordAndShield";

    private float maxHealth;
    private NavMeshAgent agent;
    private Transform player;
    private PlayerController playerController;
    private Animator animator;
    private WaveSpawner waveSpawner;

    private bool isDead = false;
    private float lastAttackTime = 0f;
    private string currentAnimation = string.Empty;


    void Start()
    {
        if (GetComponent<EnemyHealthBar>() == null)
        {
            gameObject.AddComponent<EnemyHealthBar>();
        }
        maxHealth = health;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        waveSpawner = FindFirstObjectByType<WaveSpawner>();
        lastAttackTime = Time.time;
    }

    public void InitializeStats(float waveBonusHealth, float waveBonusDamage)
    {
        health += waveBonusHealth;
        attackDamage += waveBonusDamage;
        maxHealth = health;
    }

    public float GetHealthPercent()
    {
        return health / maxHealth;
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotationSpeed);
            }

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                PlayAnimation(AttackAnim, 0.08f);

                if (playerController != null)
                {
                    playerController.TakeDamage(attackDamage);
                }
            }
            else
            {
                PlayAnimation(IdleAnim, 0.12f);
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            if (agent.velocity.magnitude > 0.1f)
            {
                PlayAnimation(MoveAnim, 0.12f);
            }
            else
            {
                PlayAnimation(IdleAnim, 0.12f);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;
        PlayAnimation(HitAnim, 0.05f);

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        agent.isStopped = true;
        agent.enabled = false;

        Collider coll = GetComponent<Collider>();
        if (coll != null) coll.enabled = false;

        PlayAnimation(DieAnim, 0.05f);

        Destroy(gameObject, 3f);

        if (waveSpawner != null)
        {
            waveSpawner.EnemyDefeated();
        }
    }

    void PlayAnimation(string stateName, float transitionDuration)
    {
        if (animator == null || currentAnimation == stateName)
        {
            return;
        }

        animator.CrossFade(stateName, transitionDuration);
        currentAnimation = stateName;
    }
}
