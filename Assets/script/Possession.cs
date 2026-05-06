using System.Linq;
using UnityEngine;

public class Possession : MonoBehaviour
{
  [Header("Pickup")]
  [SerializeField] private float pickupRadius = 3f;
  [SerializeField] private float loseRadius = 5f;

  [Header("Dribble")]
  [Tooltip("How far in front of the player the ball sits")]
  [SerializeField] private float dribbleDistance = 1.2f;
  [Tooltip("How quickly the ball corrects toward the target position")]
  [SerializeField] private float correctionSpeed = 12f;

  private Team _team;
  private Player _player;
  private Rigidbody _playerRb;
  private Collider[] _playerColliders;
  private bool _collisionIgnored = false;
  private float _pickupCooldown = 0f;

  private void Awake()
  {
    _team = GetComponentInParent<Team>();
    _player = GetComponent<Player>();
    _playerRb = GetComponent<Rigidbody>();
    _playerColliders = GetComponentsInChildren<Collider>();
  }

  private void Update()
  {
    if (Ball.Instance == null) return;

    // Ignore collision between ALL players and the ball at all times
    // so the ball never physically pushes any player
    if (!_collisionIgnored)
    {
      SetBallCollisionIgnore(true);
      _collisionIgnored = true;
    }

    // Tick cooldown (prevents re-pickup right after a pass)
    if (_pickupCooldown > 0f)
    {
      _pickupCooldown -= Time.deltaTime;
      return;
    }

    // If this player has possession, check if ball drifted too far
    if (_player.hasPossession)
    {
      float dist = HorizontalDistance(transform.position, Ball.Instance.transform.position);
      if (dist > loseRadius)
        ReleaseBall();
      return;
    }

    // If a teammate already has possession, skip
    if (AnyTeammateHasPossession()) return;

    // Proximity pickup
    float pickupDist = HorizontalDistance(transform.position, Ball.Instance.transform.position);
    if (pickupDist <= pickupRadius)
      GainPossession();
  }

    private void FixedUpdate()
    {
        if (!_player.hasPossession) return;
        if (Ball.Instance == null) return;

        // 1. Determine target position
        // If the player has a designated BallPosition transform, use it.
        // Otherwise, use a point 0.8m in front of them.
        Vector3 targetPos;
        if (_player.ballPosition != null)
        {
            targetPos = _player.ballPosition.position;
        }
        else
        {
            targetPos = transform.position + transform.forward * 0.8f;
            targetPos.y = 0.13f; 
        }

        // 2. Physics-based Dribble Smoothing
        // Use a PD-controller style approach for extremely smooth ball following
        Vector3 currentBallPos = Ball.Instance.transform.position;
        Vector3 playerVel = _playerRb.linearVelocity;
        
        // Horizontal correction
        Vector3 horizontalTarget = new Vector3(targetPos.x, 0, targetPos.z);
        Vector3 horizontalBall = new Vector3(currentBallPos.x, 0, currentBallPos.z);
        Vector3 diff = horizontalTarget - horizontalBall;
        
        // High spring force for responsiveness
        Vector3 correction = diff * correctionSpeed;
        
        // Match player velocity and add correction
        Vector3 targetVel = playerVel + correction;
        
        // Vertical logic: Keep ball grounded but allow physics to resolve
        float targetY = targetPos.y;
        float currentY = currentBallPos.y;
        float yVel = (targetY - currentY) * 15f;
        
        // Apply final velocity
        Ball.Instance.rb.linearVelocity = new Vector3(targetVel.x, yVel, targetVel.z);
    }

  private void GainPossession()
  {
    _player.hasPossession = true;
    SwapBrains();
  }

  /// <summary>
  /// Release the ball. Sets a cooldown to prevent immediate re-pickup.
  /// </summary>
  public void ReleaseBall()
  {
    if (!_player.hasPossession) return;
    _player.hasPossession = false;
    _pickupCooldown = 0.6f; // Prevent re-pickup for 0.6 seconds
  }

  private void SetBallCollisionIgnore(bool ignore)
  {
    if (Ball.Instance == null) return;
    Collider ballCol = Ball.Instance.GetComponentInChildren<Collider>();
    if (ballCol == null) return;

    foreach (var col in _playerColliders)
    {
      if (col != null)
        Physics.IgnoreCollision(col, ballCol, ignore);
    }
  }

  private bool AnyTeammateHasPossession()
  {
    return _team._teamPlayers.Any(p => p.hasPossession);
  }

  private float HorizontalDistance(Vector3 a, Vector3 b)
  {
    return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
  }

  private void SwapBrains()
  {
    if (_team._currentPlayer[0] != _player)
    {
      _team._currentPlayer[0].AiBrain();
      _team._currentPlayer[1] = _team._currentPlayer[0];
      _team._currentPlayer[0] = _player;
      _player.UserBrain();
    }
  }
}