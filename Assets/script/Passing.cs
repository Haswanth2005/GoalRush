using System.Collections;
using System.Linq;
using UnityEngine;

public class Passing : MonoBehaviour
{
  [SerializeField] private float passSpeed = 15f;

  private Team _team;

  private void Awake()
  {
    _team = GetComponent<Team>();
  }

  private void Update()
  {
    if (!Input.GetButtonUp("Pass")) return;
    if (!_team._currentPlayer[0].hasPossession) return;

    PassToPlayerInDirection(DirectionIndicator.GetMouseDirection(_team._currentPlayer[0].transform.position));
  }

  private void PassToClosestPlayer()
  {
    var target = _team._teamPlayers
        .Where(t => t != _team._currentPlayer[0])
        .OrderBy(t => Vector3.Distance(_team._currentPlayer[0].transform.position, t.transform.position))
        .FirstOrDefault();

    if (target != null) ExecutePass(target);
  }

  private void PassToPlayerInDirection(Vector3 direction)
  {
    var target = _team._teamPlayers
        .Where(t => t != _team._currentPlayer[0])
        .OrderBy(t => Vector3.Angle(direction, DirectionTo(t, _team._currentPlayer[0])))
        .FirstOrDefault();

    if (target != null) ExecutePass(target);
  }

  private void ExecutePass(Player target)
  {
    Player passer = _team._currentPlayer[0];

    // Release possession (starts cooldown so passer can't re-pick)
    Possession possession = passer.GetComponent<Possession>();
    if (possession != null)
      possession.ReleaseBall();

    // Calculate ground-level direction to target
    Vector3 direction = target.transform.position - Ball.Instance.transform.position;
    direction.y = 0f;
    direction.Normalize();

    // Set ball velocity directly for smooth, predictable pass
    Ball.Instance.rb.angularVelocity = Vector3.zero;
    Ball.Instance.rb.linearVelocity = direction * passSpeed;
  }

  private Vector3 InputDirection()
  {
    float xInput = Input.GetAxisRaw("Horizontal");
    float zInput = Input.GetAxisRaw("Vertical");
    var forward = Camera.main.transform.forward;
    var right = Camera.main.transform.right;
    forward.y = 0;
    right.y = 0;
    return (right * xInput + forward * zInput).normalized;
  }

  private Vector3 DirectionTo(Player to, Player from)
  {
    return Vector3.Normalize(to.transform.position - from.transform.position);
  }
}