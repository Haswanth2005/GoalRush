using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float rotationSpeed = 10f;
    public Animator animator;

    private Rigidbody rb;
    private Vector3 moveDirection;

    // Cached camera directions (set once since camera never rotates)
    private Vector3 _camForward;
    private Vector3 _camRight;
    private bool _directionsCached = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        // Interpolate for smooth visual movement between physics steps
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        // Reset move direction so the player doesn't keep drifting
        // from a previous input when control switches to them
        moveDirection = Vector3.zero;
    }

    private void OnDisable()
    {
        // Stop all movement when AI takes over
        moveDirection = Vector3.zero;
        if (rb != null)
            rb.linearVelocity = Vector3.zero;
    }

    private void Update()
    {
        if (Camera.main == null) return;

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

        if (animator != null)
        {
            // Use moveDirection.magnitude for the animation parameter
            animator.SetFloat("Speed", moveDirection.magnitude);
        }

        // Handle rotation: Snappy but smooth turning toward move direction
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed);
        }
    }

    private void FixedUpdate()
    {
        // 1. Calculate horizontal movement
        Vector3 horizontalMove = moveDirection * moveSpeed * Time.fixedDeltaTime;
        
        // 2. Get current vertical velocity (gravity's effect)
        // We preserve the vertical change that happened since last FixedUpdate
        Vector3 newPos = rb.position + horizontalMove;
        
        // Note: MovePosition with a non-kinematic Rigidbody will still
        // apply gravity IF we don't force the Y every frame. 
        // But the most robust way to mix manual control + gravity is velocity.
        
        Vector3 currentVel = rb.linearVelocity;
        Vector3 targetVel = moveDirection * moveSpeed;
        
        // Apply target velocity to X and Z, keep gravity on Y
        rb.linearVelocity = new Vector3(targetVel.x, currentVel.y, targetVel.z);
    }
}