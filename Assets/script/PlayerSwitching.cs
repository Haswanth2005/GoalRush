using System.Linq;
using UnityEngine;

public class PlayerSwitching : MonoBehaviour
{
  private Team _team;

  private void Awake()
  {
    _team = GetComponent<Team>();
  }

  private void Update()
  {
    if (Ball.Instance == null) return;

    // Tab: always switch to closest player to ball (works even during possession)
    if (Input.GetKeyDown(KeyCode.Tab))
    {
      SwitchToPlayerClosestToBall();
      return;
    }

    // E (Pass button): switch only when no one has possession
    if (!Input.GetButtonDown("Pass")) return;
    if (_team._teamPlayers.Any(p => p.hasPossession)) return;

    // Switch towards the mouse direction relative to the current player
    Vector3 mouseDir = DirectionIndicator.GetMouseDirection(_team._currentPlayer[0].transform.position);
    SwitchToPlayerInDirection(mouseDir);
  }

  private void SwitchToPlayer(Player target)
  {
    if (target == null || target == _team._currentPlayer[0]) return;

    // Give old player AI brain
    _team._currentPlayer[0].AiBrain();

    // Update the list: new controlled = target, previous = old controlled
    _team._currentPlayer[1] = _team._currentPlayer[0];
    _team._currentPlayer[0] = target;

    // Give new player user brain
    _team._currentPlayer[0].UserBrain();
  }

  private void SwitchToPlayerClosestToBall()
  {
    var closest = _team._teamPlayers
        .Where(t => t != _team._currentPlayer[0])
        .OrderBy(t => Vector3.Distance(Ball.Instance.transform.position, t.transform.position))
        .FirstOrDefault();

    SwitchToPlayer(closest);
  }

  private void SwitchToPlayerInDirection(Vector3 direction)
  {
    var best = _team._teamPlayers
        .Where(t => t != _team._currentPlayer[0])
        .OrderBy(t => Vector3.Angle(direction, DirectionTo(t, _team._currentPlayer[0])))
        .FirstOrDefault();

    SwitchToPlayer(best);
  }

  private Vector3 DirectionTo(Player to, Player from)
  {
    return Vector3.Normalize(to.transform.position - from.transform.position);
  }
}