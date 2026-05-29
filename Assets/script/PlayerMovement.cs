using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float rotationSpeed = 10f;
    public float acceleration = 30f;
    public float deceleration = 36f;
    public Animator animator;

    [Header("Ground Check")]
    [Tooltip("LayerMask for the ground. Excludes Player layer (16) by default.")]
    public LayerMask groundLayer = ~(1 << 16); // everything except Player layer
    public float groundCheckDistance = 0.25f;    // ray length below capsule bottom

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Vector3 moveDirection;
    private bool isGrounded;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        // Interpolate for smooth visual movement between physics steps
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        capsule = GetComponent<CapsuleCollider>();

        if (animator == null) animator = GetComponentInChildren<Animator>();

        // IMPORTANT: Disable root motion — the walk animation has baked Y movement
        // that lifts the player off the ground. We drive all movement via Rigidbody.
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Fixed;
        }
    }

    private void OnEnable()
    {
        // Reset move direction so the player doesn't keep drifting
        // from a previous input when control switches to them
        moveDirection = Vector3.zero;
        if (animator != null) animator.SetFloat(SpeedHash, 0f);
    }

    private void OnDisable()
    {
        // Stop all movement when AI takes over
        moveDirection = Vector3.zero;
        if (rb != null)
            rb.linearVelocity = Vector3.zero;
        if (animator != null) animator.SetFloat(SpeedHash, 0f);
    }

    private void Update()
    {
        if (Camera.main == null) return;

        // Block all player input outside of Playing state
        bool canPlay = GameManager.Instance == null ||
                       GameManager.Instance.currentState == GameState.Playing;

        if (!canPlay)
        {
            moveDirection = Vector3.zero;
            if (animator != null) animator.SetFloat(SpeedHash, 0f);
            return;
        }

        // Get camera-relative directions every frame for a follow camera
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        float xInput = Input.GetAxis("Horizontal");
        float zInput = Input.GetAxis("Vertical");

        // Calculate move direction relative to camera
        moveDirection = (camRight * xInput + camForward * zInput);
        
        // Clamp magnitude to 1 for consistent speed across all directions
        if (moveDirection.magnitude > 1f) moveDirection.Normalize();
    }

    private void FixedUpdate()
    {
        CheckGrounded();

        Vector3 currentVel = rb.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);
        Vector3 targetHorizontal = moveDirection * moveSpeed;
        float accel = targetHorizontal.sqrMagnitude > currentHorizontal.sqrMagnitude ? acceleration : deceleration;
        Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, targetHorizontal, accel * Time.fixedDeltaTime);

        // Rotate from actual horizontal movement to keep body direction aligned with motion.
        if (newHorizontal.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(newHorizontal.normalized);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed));
        }

        float verticalVel;
        if (isGrounded)
        {
            // Snap the player firmly to the ground without applying a constant massive downward force that causes jitter
            verticalVel = currentVel.y < -0.1f ? currentVel.y : -2f;
        }
        else
        {
            // In the air: let gravity accumulate naturally
            verticalVel = currentVel.y;
        }

        // Apply horizontal movement + resolved vertical velocity
        rb.linearVelocity = new Vector3(newHorizontal.x, verticalVel, newHorizontal.z);

        // Drive animation from actual local velocity so blend follows direction and speed.
        if (animator != null)
        {
            Vector3 horizontalVel = rb.linearVelocity;
            horizontalVel.y = 0f;
            Vector3 localVel = transform.InverseTransformDirection(horizontalVel);
            float speed01 = Mathf.Clamp01(horizontalVel.magnitude / Mathf.Max(0.01f, moveSpeed));

            animator.SetFloat(SpeedHash, speed01, 0.08f, Time.fixedDeltaTime);
            animator.SetFloat(MoveXHash, Mathf.Clamp(localVel.x / Mathf.Max(0.01f, moveSpeed), -1f, 1f), 0.08f, Time.fixedDeltaTime);
            animator.SetFloat(MoveYHash, Mathf.Clamp(localVel.z / Mathf.Max(0.01f, moveSpeed), -1f, 1f), 0.08f, Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Casts a short ray from the bottom of the capsule to detect the ground.
    /// More reliable than OnCollisionStay for slope/edge cases.
    /// </summary>
    private void CheckGrounded()
    {
        if (capsule == null)
        {
            isGrounded = false;
            return;
        }

        // Bottom-centre of the capsule in world space
        Vector3 bottom = transform.TransformPoint(capsule.center)
                         - Vector3.up * (capsule.height * 0.5f * transform.lossyScale.y - capsule.radius * transform.lossyScale.y);

        isGrounded = Physics.SphereCast(
            bottom + Vector3.up * 0.05f,    // start slightly inside to avoid tunnelling
            capsule.radius * transform.lossyScale.x * 0.9f,
            Vector3.down,
            out _,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore);
    }
}
