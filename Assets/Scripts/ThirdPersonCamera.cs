using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 1.5f, 0);
    public float distance = 5.0f;
    public float mouseSensitivity = 0.5f;
    public float rotationSmoothing = 12f;
    public float followSmoothing = 14f;

    private float currentX = 0.0f;
    private float currentY = 15.0f;
    public float minYAngle = -20.0f;
    public float maxYAngle = 80.0f;
    private bool inputLocked;

    void Start()
    {
        if (target == null)
        {
            GameObject pObj = GameObject.FindWithTag("Player");
            if (pObj != null) target = pObj.transform;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            GameObject pObj = GameObject.FindWithTag("Player");
            if (pObj != null) target = pObj.transform;
            if (target == null) return;
        }

        if (!inputLocked && Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            currentX += mouseDelta.x * mouseSensitivity;
            currentY -= mouseDelta.y * mouseSensitivity;
        }

        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);

        Vector3 dir = new Vector3(0, 0, -distance);
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        Vector3 desiredPosition = target.position + offset + rotation * dir;

        Vector3 resolvedPosition = desiredPosition;
        if (Physics.Linecast(target.position + offset, desiredPosition, out RaycastHit hit))
        {
            bool hitPlayer = hit.collider.CompareTag("Player");
            bool hitEnemy = hit.collider.GetComponentInParent<EnemyController>() != null;
            if (!hitPlayer && !hitEnemy && !hit.collider.isTrigger)
            {
                resolvedPosition = hit.point + hit.normal * 0.15f;
            }
        }

        transform.position = Vector3.Lerp(transform.position, resolvedPosition, 1f - Mathf.Exp(-followSmoothing * Time.deltaTime));
        Quaternion lookRotation = Quaternion.LookRotation((target.position + offset) - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime));
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
    }
}
