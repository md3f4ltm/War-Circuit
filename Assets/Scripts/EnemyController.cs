using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    private static readonly HashSet<EnemyController> ActiveEnemies = new HashSet<EnemyController>();

    public float health = 30f;
    public float attackRange = 2.0f;
    public float attackCooldown = 1.6f;
    public float attackDamage = 5f;
    public float attackAnimationDuration = 0.8f;
    public float attackHitDelay = 0.45f;
    public float rotationSpeed = 8f;
    public float modelForwardOffset = 0f;

    private static readonly string[] IdleAnimCandidates =
    {
        "Idle",
        "Idle_Battle_SwordAndShiled",
        "rig_Anim,Idle"
    };

    private static readonly string[] MoveAnimCandidates =
    {
        "Walk",
        "MoveFWD_Normal_InPlace_SwordAndShield",
        "rig_Anim_WALK"
    };

    private static readonly string[] AttackAnimCandidates =
    {
        "Attack",
        "Attack01_SwordAndShiled",
        "rig_Anim_Attack_01"
    };

    private static readonly string[] HitAnimCandidates =
    {
        "GetHit01_SwordAndShield",
        "rig_Anim,Idle"
    };

    private static readonly string[] DieAnimCandidates =
    {
        "Die",
        "Die01_SwordAndShield",
        "rig_Anim_Die"
    };

    private float maxHealth;
    private float baseAgentSpeed;
    private float burnTimeRemaining;
    private float burnTickTimer;
    private float burnDamagePerSecond;
    private float slowTimeRemaining;
    private float slowMoveMultiplier = 1f;
    private NavMeshAgent agent;
    private Transform player;
    private PlayerController playerController;
    private Animator animator;
    private WaveSpawner waveSpawner;

    private bool isDead = false;
    private bool hasAppliedAttackDamage = false;
    private float lastAttackTime = 0f;
    private string currentAnimation = string.Empty;
    private string idleAnim = string.Empty;
    private string moveAnim = string.Empty;
    private string attackAnim = string.Empty;
    private string hitAnim = string.Empty;
    private string dieAnim = string.Empty;

    void OnEnable()
    {
        ActiveEnemies.Add(this);
    }

    void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    void OnDestroy()
    {
        ActiveEnemies.Remove(this);
    }

    void Start()
    {
        if (GetComponent<EnemyHealthBar>() == null)
        {
            gameObject.AddComponent<EnemyHealthBar>();
        }
        maxHealth = health;
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        baseAgentSpeed = agent.speed;
        animator = ResolveAnimator();

        if (animator != null)
        {
            animator.applyRootMotion = false;
            idleAnim = ResolveAnimationName(IdleAnimCandidates);
            moveAnim = ResolveAnimationName(MoveAnimCandidates);
            attackAnim = ResolveAnimationName(AttackAnimCandidates);
            hitAnim = ResolveAnimationName(HitAnimCandidates, idleAnim);
            dieAnim = ResolveAnimationName(DieAnimCandidates);
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

        UpdateStatusEffects();
        bool isMoving = false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            RotateTowards(direction);

            float timeSinceLastAttack = Time.time - lastAttackTime;

            if (timeSinceLastAttack < attackAnimationDuration)
            {
                PlayAnimation(attackAnim, 0.05f);

                if (!hasAppliedAttackDamage && timeSinceLastAttack >= attackHitDelay)
                {
                    hasAppliedAttackDamage = true;

                    if (playerController != null && distanceToPlayer <= attackRange + 0.2f)
                    {
                        playerController.TakeDamage(attackDamage);
                    }
                }
            }
            else if (timeSinceLastAttack >= attackCooldown)
            {
                lastAttackTime = Time.time;
                hasAppliedAttackDamage = false;
                PlayAnimation(attackAnim, 0.08f, true);
            }
            else
            {
                PlayAnimation(idleAnim, 0.12f);
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            RotateTowards(agent.desiredVelocity);

            float navigationSpeed = Mathf.Max(agent.velocity.magnitude, agent.desiredVelocity.magnitude);
            isMoving = navigationSpeed > 0.03f && (!agent.hasPath || agent.remainingDistance > agent.stoppingDistance);

            if (isMoving)
            {
                PlayAnimation(moveAnim, 0.12f);
            }
            else
            {
                PlayAnimation(idleAnim, 0.12f);
            }
        }

        UpdateAnimationPlaybackSpeed(isMoving);
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, true);
    }

    public void TakeDamage(float damage, bool playHitReaction)
    {
        if (isDead) return;

        health -= damage;
        if (playHitReaction)
        {
            PlayAnimation(hitAnim, 0.05f, true);
        }

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        ActiveEnemies.Remove(this);
        agent.isStopped = true;
        agent.enabled = false;

        Collider coll = GetComponent<Collider>();
        if (coll != null) coll.enabled = false;

        PlayAnimation(dieAnim, 0.05f, true);

        Destroy(gameObject, 3f);

        if (waveSpawner != null)
        {
            waveSpawner.EnemyDefeated(transform.position);
        }
    }

    void UpdateStatusEffects()
    {
        if (burnTimeRemaining > 0f)
        {
            burnTimeRemaining -= Time.deltaTime;
            burnTickTimer -= Time.deltaTime;

            if (burnTickTimer <= 0f)
            {
                burnTickTimer = 1f;
                TakeDamage(burnDamagePerSecond, false);
            }
        }

        if (slowTimeRemaining > 0f)
        {
            slowTimeRemaining -= Time.deltaTime;
        }
        else
        {
            slowMoveMultiplier = 1f;
        }

        if (agent != null && agent.enabled)
        {
            agent.speed = baseAgentSpeed * slowMoveMultiplier;
        }
    }

    public void ApplyBurn(float duration, float damagePerSecond)
    {
        if (isDead || duration <= 0f || damagePerSecond <= 0f)
        {
            return;
        }

        burnTimeRemaining = Mathf.Max(burnTimeRemaining, duration);
        burnDamagePerSecond = Mathf.Max(burnDamagePerSecond, damagePerSecond);
        burnTickTimer = Mathf.Min(burnTickTimer, 0.15f);
    }

    public void ApplySlow(float duration, float moveMultiplier)
    {
        if (isDead || duration <= 0f)
        {
            return;
        }

        slowTimeRemaining = Mathf.Max(slowTimeRemaining, duration);
        slowMoveMultiplier = Mathf.Clamp(Mathf.Min(slowMoveMultiplier, moveMultiplier), 0.2f, 1f);
    }

    void UpdateAnimationPlaybackSpeed(bool isMoving)
    {
        if (animator == null)
        {
            return;
        }

        animator.speed = isMoving ? Mathf.Clamp(slowMoveMultiplier, 0.45f, 1f) : 1f;
    }

    public static EnemyController FindClosestAlive(Vector3 worldPosition, float maxDistance)
    {
        EnemyController closestEnemy = null;
        float closestDistanceSqr = maxDistance * maxDistance;

        foreach (EnemyController enemy in ActiveEnemies)
        {
            if (enemy == null || enemy.isDead)
            {
                continue;
            }

            float distanceSqr = (enemy.transform.position - worldPosition).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    void RotateTowards(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized) * Quaternion.Euler(0f, modelForwardOffset, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    Animator ResolveAnimator()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        Animator preferredAnimator = null;

        foreach (Animator candidate in animators)
        {
            if (candidate == null)
            {
                continue;
            }

            if (candidate.transform == transform)
            {
                preferredAnimator = candidate;
                break;
            }

            preferredAnimator ??= candidate;
        }

        foreach (Animator candidate in animators)
        {
            if (candidate == null)
            {
                continue;
            }

            candidate.enabled = candidate == preferredAnimator;
        }

        return preferredAnimator;
    }

    void PlayAnimation(string stateName, float transitionDuration, bool restartIfSame = false)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        if (restartIfSame && currentAnimation == stateName)
        {
            if (TryPlayFromStart(stateName))
            {
                return;
            }
        }

        if (currentAnimation == stateName)
        {
            if (HasCurrentAnimationFinished() && TryPlayFromStart(stateName))
            {
                return;
            }

            return;
        }

        if (TryCrossFade(stateName, transitionDuration))
        {
            currentAnimation = stateName;
        }
    }

    bool TryCrossFade(string stateName, float transitionDuration)
    {
        int shortHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, shortHash))
        {
            animator.CrossFade(shortHash, transitionDuration, 0);
            return true;
        }

        string fullPath = $"Base Layer.{stateName}";
        int fullHash = Animator.StringToHash(fullPath);
        if (animator.HasState(0, fullHash))
        {
            animator.CrossFade(fullHash, transitionDuration, 0);
            return true;
        }

        return false;
    }

    bool TryPlayFromStart(string stateName)
    {
        int shortHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, shortHash))
        {
            animator.Play(shortHash, 0, 0f);
            animator.Update(0f);
            currentAnimation = stateName;
            return true;
        }

        int fullHash = Animator.StringToHash($"Base Layer.{stateName}");
        if (animator.HasState(0, fullHash))
        {
            animator.Play(fullHash, 0, 0f);
            animator.Update(0f);
            currentAnimation = stateName;
            return true;
        }

        return false;
    }

    bool HasCurrentAnimationFinished()
    {
        if (animator == null || animator.IsInTransition(0))
        {
            return false;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.normalizedTime >= 1f;
    }

    string ResolveAnimationName(string[] candidates, string fallback = "")
    {
        foreach (string candidate in candidates)
        {
            if (HasAnimationState(candidate))
            {
                return candidate;
            }
        }

        return fallback;
    }

    bool HasAnimationState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        int shortHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, shortHash))
        {
            return true;
        }

        int fullHash = Animator.StringToHash($"Base Layer.{stateName}");
        return animator.HasState(0, fullHash);
    }
}
