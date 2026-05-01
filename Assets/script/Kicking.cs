using UnityEngine;

public class Kicking : MonoBehaviour
{
    [Header("Kick Power")]
    [Tooltip("Minimum kick force (tap Space)")]
    [SerializeField] private float minKickForce = 12f;
    [Tooltip("Maximum kick force (fully charged)")]
    [SerializeField] private float maxKickForce = 35f;
    [Tooltip("Seconds to reach full charge")]
    [SerializeField] private float chargeTime = 1.2f;

    [Header("Kick Angle")]
    [Tooltip("Upward angle in degrees for a slight loft")]
    [SerializeField] private float kickUpAngle = 12f;

    private Team _team;
    private bool _isCharging = false;
    private float _chargeTimer = 0f;

    /// <summary>
    /// Current charge percentage (0–1). Used by DirectionIndicator for visual feedback.
    /// </summary>
    public float ChargePercent => _isCharging ? Mathf.Clamp01(_chargeTimer / chargeTime) : 0f;
    public bool IsCharging => _isCharging;

    private void Awake()
    {
        _team = GetComponent<Team>();
    }

    private void Update()
    {
        if (_team._currentPlayer.Count == 0) return;

        Player current = _team._currentPlayer[0];

        // Cancel charge if the player loses possession mid-charge
        if (!current.hasPossession)
        {
            _isCharging = false;
            _chargeTimer = 0f;
            return;
        }

        // Start charging on Space press
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isCharging = true;
            _chargeTimer = 0f;
        }

        // Accumulate charge while holding
        if (_isCharging && Input.GetKey(KeyCode.Space))
        {
            _chargeTimer += Time.deltaTime;
            _chargeTimer = Mathf.Min(_chargeTimer, chargeTime);
        }

        // Execute kick on release
        if (Input.GetKeyUp(KeyCode.Space) && _isCharging)
        {
            ExecuteKick();
            _isCharging = false;
            _chargeTimer = 0f;
        }
    }

    private void ExecuteKick()
    {
        Player kicker = _team._currentPlayer[0];
        if (Ball.Instance == null) return;

        // Direction toward the mouse cursor (horizontal)
        Vector3 direction = DirectionIndicator.GetMouseDirection(kicker.transform.position);

        // Add a slight upward loft so the ball lifts off the ground
        float upComponent = Mathf.Tan(kickUpAngle * Mathf.Deg2Rad);
        direction = (direction + Vector3.up * upComponent).normalized;

        // Force scales with how long Space was held
        float charge = Mathf.Clamp01(_chargeTimer / chargeTime);
        float kickForce = Mathf.Lerp(minKickForce, maxKickForce, charge);

        // Release possession first (sets cooldown to prevent instant re-pickup)
        Possession possession = kicker.GetComponent<Possession>();
        if (possession != null)
            possession.ReleaseBall();

        // Apply the kick
        Ball.Instance.rb.angularVelocity = Vector3.zero;
        Ball.Instance.rb.linearVelocity = direction * kickForce;
    }
}
