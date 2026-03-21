using UnityEngine;
using Benjathemaker;

public class HealthPickup : MonoBehaviour
{
    public float healAmount = 35f;
    public float lifetime = 15f;
    public Vector3 worldScale = new Vector3(0.28f, 0.28f, 0.28f);
    public Color primaryColor = new Color(0.9f, 0.15f, 0.2f, 1f);

    void Awake()
    {
        transform.localScale = worldScale;

        Collider pickupCollider = GetComponent<Collider>();
        if (pickupCollider == null)
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = 0.9f;
            pickupCollider = sphereCollider;
        }

        pickupCollider.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;

        ApplyVisualTuning();
    }

    void OnEnable()
    {
        CancelInvoke(nameof(DestroyPickup));
        Invoke(nameof(DestroyPickup), lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        player ??= other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        player.Heal(healAmount);
        Destroy(gameObject);
    }

    void DestroyPickup()
    {
        Destroy(gameObject);
    }

    void ApplyVisualTuning()
    {
        SimpleGemsAnim gemAnim = GetComponent<SimpleGemsAnim>();
        if (gemAnim != null)
        {
            gemAnim.floatHeight = 0.18f;
            gemAnim.floatSpeed = 0.9f;
            gemAnim.rotationSpeed = 60f;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material runtimeMaterial = renderer.material;
            if (runtimeMaterial.HasProperty("_BaseColor"))
            {
                runtimeMaterial.SetColor("_BaseColor", primaryColor);
            }
            else if (runtimeMaterial.HasProperty("_Color"))
            {
                runtimeMaterial.color = primaryColor;
            }
        }
    }
}
