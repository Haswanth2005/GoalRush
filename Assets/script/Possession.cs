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

    // Target: in front of the player based on facing direction
    Vector3 targetPos = transform.position + transform.forward * dribbleDistance;
    targetPos.y = Ball.Instance.transform.position.y;

    // Offset from ball to where it should be
    Vector3 offset = targetPos - Ball.Instance.transform.position;
    offset.y = 0f;

    // Base velocity = player's velocity (ball moves WITH the player)
    Vector3 playerVel = _playerRb != null ? _playerRb.linearVelocity : Vector3.zero;
    playerVel.y = 0f;

    // Correction pushes ball toward the front position
    Vector3 correction = offset * correctionSpeed;

    // Desired horizontal velocity
    Vector3 desiredVel = playerVel + correction;

    // Smoothly blend to desired velocity, preserve gravity
    Vector3 currentVel = Ball.Instance.rb.linearVelocity;
    float gravityY = currentVel.y;

    Vector3 newVel = Vector3.Lerp(currentVel, desiredVel, Time.fixedDeltaTime * 15f);
    newVel.y = gravityY;

    Ball.Instance.rb.linearVelocity = newVel;
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