using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float turnSmoothTime = 0.1f;
    public float gravity = -38f;
    public float jumpHeight = 1.5f;
    public float doubleJumpHeight = 1.25f;
    public int maxJumpCount = 2;
    public float doubleJumpFlipDuration = 0.45f;
    public float doubleJumpFlipDegrees = 360f;
    public float groundedOffset = 0.08f;
    public float groundedRadius = 0.25f;
    public LayerMask groundLayers = ~0;

    public float maxHealth = 220f;
    public float attackDuration = 0.6f;
    public float attackRange = 2f;
    public float attackRadius = 1.6f;
    public float attackDamage = 40f;

    private const string IdleAnim = "Idle_Normal_SwordAndShield";
    private const string MoveAnim = "MoveFWD_Normal_InPlace_SwordAndShield";
    private const string RunAnim = "SprintFWD_Battle_InPlace_SwordAndShield";
    private const string AttackAnim = "Attack01_SwordAndShiled";
    private const string HitAnim = "GetHit01_SwordAndShield";
    private const string DieAnim = "Die01_SwordAndShield";
    private const string JumpAnim = "JumpFull_Normal_RM_SwordAndShield";
    private const string DoubleJumpAnim = "JumpFull_Spin_RM_SwordAndShield";

    private float turnSmoothVelocity;
    private float currentHealth;
    private float attackTimer;
    private float currentYaw;
    private float currentPitch;
    private float flipTimer;
    private Vector3 verticalVelocity;
    private Vector3 planarVelocity;
    private float airborneSpeed;

    private CharacterController controller;
    private Animator animator;
    private Transform cam;
    private Transform visualRoot;
    private Quaternion visualRootBaseRotation;

    private bool isDead;
    private bool isAttacking;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool hasUsedAirJump;
    private bool isPerformingFlip;
    private string currentAnimation = string.Empty;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>(true);
        cam = Camera.main != null ? Camera.main.transform : null;

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        visualRoot = transform.Find("root");
        if (visualRoot != null)
        {
            visualRootBaseRotation = visualRoot.localRotation;
        }

        currentYaw = transform.eulerAngles.y;
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (cam == null && Camera.main != null)
        {
            cam = Camera.main.transform;
        }

        if (isDead)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        UpdateGroundedState();
        HandleCombatState();
        HandleMovement();
        UpdateAirRotation();
        HandleCombat();
        UpdateAnimationState();
    }

    void UpdateGroundedState()
    {
        wasGroundedLastFrame = isGrounded;

        bool controllerGrounded = controller != null && controller.isGrounded;
        isGrounded = controllerGrounded;

        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        if (!wasGroundedLastFrame && isGrounded)
        {
            hasUsedAirJump = false;
            isPerformingFlip = false;
            currentPitch = 0f;
            ResetVisualRotation();
        }
    }

    void HandleCombatState()
    {
        if (!isAttacking)
        {
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            isAttacking = false;
        }
    }

    void HandleMovement()
    {
        Vector2 moveInput = ReadMoveInput();
        bool hasMoveInput = moveInput.sqrMagnitude > 0.01f;
        bool runHeld = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        bool wantsJump = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool isRunning = runHeld && hasMoveInput && isGrounded && !isAttacking;
        float speedMultiplier = isAttacking ? 0.45f : 1f;
        float groundedSpeed = (isRunning ? runSpeed : moveSpeed) * speedMultiplier;

        if (isGrounded)
        {
            airborneSpeed = groundedSpeed;
        }

        bool canGroundJump = isGrounded;
        bool canAirJump = !isGrounded && maxJumpCount > 1 && !hasUsedAirJump;

        if (wantsJump && !isAttacking && (canGroundJump || canAirJump))
        {
            bool isDoubleJump = !canGroundJump;
            float selectedJumpHeight = isDoubleJump ? doubleJumpHeight : jumpHeight;

            if (canGroundJump)
            {
                airborneSpeed = groundedSpeed;
            }

            isGrounded = false;
            if (isDoubleJump)
            {
                hasUsedAirJump = true;
                isPerformingFlip = true;
                flipTimer = doubleJumpFlipDuration;
            }

            verticalVelocity.y = Mathf.Sqrt(selectedJumpHeight * -2f * gravity);
            PlayAnimation(isDoubleJump ? DoubleJumpAnim : JumpAnim, 0.06f);
        }

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 moveDirection = GetCameraRelativeDirection(inputDirection);

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        }
        float horizontalSpeed = isGrounded ? groundedSpeed : airborneSpeed;

        planarVelocity = moveDirection * horizontalSpeed;

        verticalVelocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = planarVelocity + Vector3.up * verticalVelocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }

    void UpdateAirRotation()
    {
        if (isGrounded)
        {
            currentPitch = 0f;
            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
            ResetVisualRotation();
            return;
        }

        if (isPerformingFlip)
        {
            flipTimer -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(flipTimer / Mathf.Max(0.01f, doubleJumpFlipDuration));
            currentPitch = Mathf.Lerp(0f, doubleJumpFlipDegrees, progress);

            if (flipTimer <= 0f)
            {
                isPerformingFlip = false;
                currentPitch = 0f;
            }
        }

        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        ApplyVisualRotation();
    }

    void ApplyVisualRotation()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localRotation = visualRootBaseRotation * Quaternion.Euler(currentPitch, 0f, 0f);
    }

    void ResetVisualRotation()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localRotation = visualRootBaseRotation;
    }

    void UpdateAnimationState()
    {
        if (animator == null || isDead)
        {
            return;
        }

        if (!isGrounded)
        {
            return;
        }

        if (isAttacking)
        {
            PlayAnimation(AttackAnim, 0.08f);
            return;
        }

        if (planarVelocity.sqrMagnitude > 0.01f)
        {
            float speed = planarVelocity.magnitude;
            PlayAnimation(speed > moveSpeed + 0.3f ? RunAnim : MoveAnim, 0.1f);
            return;
        }

        PlayAnimation(IdleAnim, 0.12f);
    }

    void HandleCombat()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isAttacking)
        {
            isAttacking = true;
            attackTimer = attackDuration;

            PlayAnimation(AttackAnim, 0.06f);

            Vector3 attackCenter = transform.position + Vector3.up * 1f + transform.forward * (attackRange - 0.6f);
            Collider[] hitColliders = Physics.OverlapSphere(attackCenter, attackRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider hitCollider in hitColliders)
            {
                EnemyController enemy = hitCollider.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(attackDamage);
                }
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        PlayAnimation(HitAnim, 0.05f);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        currentHealth = 0;
        PlayAnimation(DieAnim, 0.05f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    Vector2 ReadMoveInput()
    {
        Vector2 moveInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > moveInput.sqrMagnitude)
            {
                moveInput = stick;
            }
        }

        return Vector2.ClampMagnitude(moveInput, 1f);
    }

    Vector3 GetCameraRelativeDirection(Vector3 inputDirection)
    {
        if (inputDirection.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        if (cam == null)
        {
            return inputDirection.normalized;
        }

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;
        forward.y = 0f;
        right.y = 0f;

        return (forward.normalized * inputDirection.z + right.normalized * inputDirection.x).normalized;
    }

    void PlayAnimation(string stateName, float transitionDuration)
    {
        if (animator == null || currentAnimation == stateName)
        {
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * groundedOffset, groundedRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f + transform.forward * (attackRange - 0.6f), attackRadius);
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;

        GUI.Box(new Rect(20, 20, 250, 40), "");
        GUI.color = Color.red;
        GUI.Label(new Rect(30, 25, 200, 30), $"Health: {Mathf.Ceil(currentHealth)} / {maxHealth}", style);
        GUI.color = Color.white;

        if (isDead)
        {
            GUIStyle deadStyle = new GUIStyle();
            deadStyle.fontSize = 64;
            deadStyle.normal.textColor = Color.red;
            deadStyle.fontStyle = FontStyle.Bold;
            deadStyle.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "YOU DIED\nPress 'R' to Restart", deadStyle);
        }

        GUIStyle hintStyle = new GUIStyle(style);
        hintStyle.fontSize = 18;
        GUI.Label(new Rect(20, 65, 360, 30), "WASD move  Shift sprint  Space jump  LMB attack", hintStyle);
    }
}
