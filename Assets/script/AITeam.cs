using System.Collections;
using UnityEngine;

/// <summary>
/// Manages which AI player actively chases the ball.
/// Periodically re-assigns the chaser to the AI player closest to the ball.
/// </summary>
public class AITeam : MonoBehaviour
{
    [Header("References")]
    [Tooltip("A transform placed at the centre of the PLAYER's goal mouth (what AI aims at).")]
    public Transform playerGoalTarget;

    [Header("Settings")]
    public float reassignInterval = 1.5f;   // seconds between chaser switches

    private AIPlayerController[] _players;
    private int _chaserIndex = 0;

    // ─────────────────────────────────────────────
    private void Start()
    {
        _players = GetComponentsInChildren<AIPlayerController>();

        foreach (var p in _players)
        {
            p.playerGoalTarget = playerGoalTarget;
            // Spread support offsets so players don't stack
            int i = System.Array.IndexOf(_players, p);
            p.supportOffset = new Vector3((i % 2 == 0 ? -4f : 4f), 0f, 4f + i * 2f);
        }

        StartCoroutine(ReassignChaser());
    }

    // ─────────────────────────────────────────────
    private IEnumerator ReassignChaser()
    {
        while (true)
        {
            yield return new WaitForSeconds(reassignInterval);

            if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Playing)
                continue;
            if (Ball.Instance == null || _players == null || _players.Length == 0)
                continue;

            // Find closest AI player to ball
            int   best     = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _players.Length; i++)
            {
                if (_players[i] == null) continue;
                float d = Vector3.Distance(_players[i].transform.position,
                                           Ball.Instance.transform.position);
                if (d < bestDist) { bestDist = d; best = i; }
            }

            // Update states
            for (int i = 0; i < _players.Length; i++)
            {
                if (_players[i] == null) continue;
                _players[i].state = (i == best)
                    ? AIPlayerController.AIState.ChaseBall
                    : AIPlayerController.AIState.Idle;
            }

            _chaserIndex = best;
        }
    }

    // ─────────────────────────────────────────────
    /// <summary>Stop all AI players (called when game paused / over).</summary>
    public void StopAll()
    {
        if (_players == null) return;
        foreach (var p in _players)
            if (p != null) p.state = AIPlayerController.AIState.Idle;
    }
}
