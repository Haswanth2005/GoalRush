using UnityEngine;

/// <summary>
/// Place this on an invisible trigger collider behind each goal mouth.
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    [Tooltip("TRUE  → this is the AI goal, so the PLAYER scores.\n" +
             "FALSE → this is the Player goal, so the AI scores.")]
    public bool playerScoresHere = true;

    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.currentState != GameState.Playing) return;
        if (!other.CompareTag("Ball")) return;

        GameManager.Instance.GoalScored(playerScoresHere);
    }
}
