using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerOrbController : MonoBehaviour
{
    private const string FireTemplateName = "BasicOrbTemplate";
    private const string FreezeTemplateName = "LightOrbTemplate";

    public float orbitRadius = 1.8f;
    public float orbitHeight = 1.25f;
    public float orbitSpinSpeed = 95f;
    public float freezeOrbitRadiusMultiplier = 1.35f;
    public float freezeOrbitHeightOffset = -0.2f;
    public float freezeAuraRadius = 1.35f;
    public float attackInterval = 2f;
    public float attackRange = 18f;
    public float orbBaseDamage = 12f;
    public float fireImpactDamageBonus = 10f;
    public float fireBurnDuration = 3.5f;
    public float fireBurnDamagePerSecond = 6f;
    public float freezeSlowDuration = 2.5f;
    public float freezeMoveMultiplier = 0.55f;
    public float projectileSpeed = 18f;
    public float projectileLifetime = 4f;
    public float orbitVisualScale = 0.28f;
    public float projectileVisualScale = 0.42f;

    private readonly List<OrbData> orbs = new List<OrbData>();
    private static Material sharedTrailMaterial;
    private PlayerController playerController;
    private float nextVolleyTime;

    enum OrbType
    {
        Fire,
        Freeze
    }

    class OrbData
    {
        public OrbType Type;
        public GameObject Visual;
    }

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (orbs.Count == 0)
        {
            return;
        }

        UpdateOrbPositions();

        if (playerController != null && playerController.IsGameplayLocked())
        {
            return;
        }

        ApplyFreezeAuras();

        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame && Time.time >= nextVolleyTime)
        {
            FireOrbVolley();
        }
    }

    public void AddFireOrb()
    {
        AddOrb(OrbType.Fire);
    }

    public void AddFreezeOrb()
    {
        AddOrb(OrbType.Freeze);
    }

    void AddOrb(OrbType orbType)
    {
        GameObject visual = CreateOrbVisual(orbType);
        orbs.Add(new OrbData
        {
            Type = orbType,
            Visual = visual
        });
        UpdateOrbPositions();
    }

    void UpdateOrbPositions()
    {
        float angleStep = 360f / Mathf.Max(1, orbs.Count);
        float baseAngle = Time.time * orbitSpinSpeed;

        for (int i = 0; i < orbs.Count; i++)
        {
            OrbData orb = orbs[i];
            if (orb.Visual == null)
            {
                continue;
            }

            float angle = baseAngle + angleStep * i;
            float currentOrbitRadius = orb.Type == OrbType.Freeze
                ? orbitRadius * freezeOrbitRadiusMultiplier
                : orbitRadius;
            float currentOrbitHeight = orb.Type == OrbType.Freeze
                ? orbitHeight + freezeOrbitHeightOffset
                : orbitHeight;
            Vector3 orbitOffset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * currentOrbitRadius;
            orb.Visual.transform.position = transform.position + orbitOffset + Vector3.up * currentOrbitHeight;

            float spinAngle = Time.time * 240f;
            float pulse = 1f + Mathf.Sin(Time.time * 5f + i * 0.9f) * 0.08f;
            orb.Visual.transform.rotation = Quaternion.Euler(spinAngle * 0.35f, -angle + spinAngle, spinAngle * 0.2f);
            orb.Visual.transform.localScale = Vector3.one * (orbitVisualScale * pulse);
        }
    }

    void ApplyFreezeAuras()
    {
        for (int i = 0; i < orbs.Count; i++)
        {
            OrbData orb = orbs[i];
            if (orb.Type != OrbType.Freeze || orb.Visual == null)
            {
                continue;
            }

            Vector3 auraCenter = orb.Visual.transform.position;
            Collider[] hitColliders = Physics.OverlapSphere(auraCenter, freezeAuraRadius, ~0, QueryTriggerInteraction.Ignore);
            for (int hitIndex = 0; hitIndex < hitColliders.Length; hitIndex++)
            {
                EnemyController enemy = hitColliders[hitIndex].GetComponent<EnemyController>();
                enemy ??= hitColliders[hitIndex].GetComponentInParent<EnemyController>();
                if (enemy != null)
                {
                    Vector3 enemyPosition = enemy.transform.position + Vector3.up * 0.9f;
                    float horizontalDistance = Vector2.Distance(
                        new Vector2(enemyPosition.x, enemyPosition.z),
                        new Vector2(auraCenter.x, auraCenter.z));
                    float verticalDistance = Mathf.Abs(enemyPosition.y - auraCenter.y);

                    if (horizontalDistance <= freezeAuraRadius + 0.25f && verticalDistance <= 1.4f)
                    {
                        enemy.ApplySlow(freezeSlowDuration, freezeMoveMultiplier);
                    }
                }
            }
        }
    }

    void FireOrbVolley()
    {
        nextVolleyTime = Time.time + attackInterval;

        for (int i = 0; i < orbs.Count; i++)
        {
            OrbData orb = orbs[i];
            if (orb.Visual == null || orb.Type != OrbType.Fire)
            {
                continue;
            }

            LaunchProjectile(orb);
        }
    }

    void LaunchProjectile(OrbData orb)
    {
        EnemyController target = EnemyController.FindClosestAlive(orb.Visual.transform.position, attackRange);
        Vector3 shootDirection = GetShootDirection(orb.Visual.transform.position, target);

        GameObject projectileRoot = new GameObject($"{orb.Type}OrbProjectile");
        projectileRoot.transform.position = orb.Visual.transform.position;

        SphereCollider collider = projectileRoot.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.4f;

        Rigidbody body = projectileRoot.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        OrbProjectile projectile = projectileRoot.AddComponent<OrbProjectile>();
        projectile.Initialize(
            this,
            target,
            shootDirection,
            projectileSpeed,
            projectileLifetime,
            orbBaseDamage + fireImpactDamageBonus,
            orb.Type == OrbType.Fire ? fireBurnDuration : 0f,
            orb.Type == OrbType.Fire ? fireBurnDamagePerSecond : 0f);

        GameObject projectileVisual = CreateOrbInstance(orb.Type, projectileVisualScale);
        if (projectileVisual != null)
        {
            projectileVisual.transform.SetParent(projectileRoot.transform, false);
        }

        AddProjectileTrail(projectileRoot, orb.Type);
    }

    void AddProjectileTrail(GameObject projectileRoot, OrbType orbType)
    {
        TrailRenderer trail = projectileRoot.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.startWidth = 0.22f;
        trail.endWidth = 0.02f;
        trail.minVertexDistance = 0.03f;
        trail.alignment = LineAlignment.View;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.autodestruct = false;

        Gradient gradient = new Gradient();
        if (orbType == OrbType.Fire)
        {
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.8f, 0.35f), 0f),
                    new GradientColorKey(new Color(1f, 0.35f, 0.08f), 0.55f),
                    new GradientColorKey(new Color(0.75f, 0.08f, 0.02f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.45f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
        }
        else
        {
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                    new GradientColorKey(new Color(0.82f, 0.92f, 1f), 0.6f),
                    new GradientColorKey(new Color(0.68f, 0.82f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.4f, 0.65f),
                    new GradientAlphaKey(0f, 1f)
                });
        }

        trail.colorGradient = gradient;

        Material trailMaterial = GetTrailMaterial();
        if (trailMaterial != null)
        {
            trail.sharedMaterial = trailMaterial;
        }
    }

    GameObject CreateOrbVisual(OrbType orbType)
    {
        GameObject visual = CreateOrbInstance(orbType, orbitVisualScale);
        visual.name = $"{orbType}OrbVisual";
        visual.transform.SetParent(transform, true);
        return visual;
    }

    GameObject CreateOrbInstance(OrbType orbType, float scale)
    {
        GameObject template = RuntimeSceneTemplateLibrary.FindSceneTemplate(GetTemplateName(orbType));
        GameObject visual = template != null
            ? Instantiate(template, transform.position, Quaternion.identity)
            : CreateFallbackOrb(orbType);

        visual.transform.localScale = Vector3.one * scale;
        visual.SetActive(true);

        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        foreach (ParticleSystem particles in visual.GetComponentsInChildren<ParticleSystem>(true))
        {
            particles.Play();
        }

        return visual;
    }

    string GetTemplateName(OrbType orbType)
    {
        return orbType == OrbType.Fire ? FireTemplateName : FreezeTemplateName;
    }

    Vector3 GetShootDirection(Vector3 spawnPosition, EnemyController target)
    {
        if (target != null)
        {
            return (target.transform.position + Vector3.up * 1f - spawnPosition).normalized;
        }

        if (Camera.main != null)
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude > 0.001f)
            {
                return cameraForward.normalized;
            }
        }

        return transform.forward;
    }

    GameObject CreateFallbackOrb(OrbType orbType)
    {
        GameObject fallbackOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fallbackOrb.transform.localScale = Vector3.one;

        Renderer renderer = fallbackOrb.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = orbType == OrbType.Fire
                ? new Color(1f, 0.4f, 0.12f)
                : new Color(0.96f, 0.98f, 1f);
        }

        return fallbackOrb;
    }

    Material GetTrailMaterial()
    {
        if (sharedTrailMaterial != null)
        {
            return sharedTrailMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            return null;
        }

        sharedTrailMaterial = new Material(shader)
        {
            name = "OrbTrailMaterial"
        };

        return sharedTrailMaterial;
    }

    void OnDestroy()
    {
        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i].Visual != null)
            {
                Destroy(orbs[i].Visual);
            }
        }
    }
}
