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
        // Cache the camera's forward/right ONCE since it never rotates.
        // Reading it every frame creates a feedback loop with the CameraController
        // that causes visible jitter.
        if (!_directionsCached && Camera.main != null)
        {
            _camForward = Camera.main.transform.forward;
            _camRight = Camera.main.transform.right;
            _camForward.y = 0f;
            _camRight.y = 0f;
            _camForward.Normalize();
            _camRight.Normalize();
            _directionsCached = true;
        }

        float xInput = Input.GetAxis("Horizontal");
        float zInput = Input.GetAxis("Vertical");

        moveDirection = (_camRight * xInput + _camForward * zInput).normalized;

        if (animator != null)
        {
            animator.SetFloat("Speed", moveDirection.magnitude);
        }

        // Handle rotation in Update for smoother visual turning
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDirection),
                Time.deltaTime * rotationSpeed);
        }
    }

    private void FixedUpdate()
    {
        // Use MovePosition for smooth physics-based movement
        Vector3 newPos = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);
    }
}