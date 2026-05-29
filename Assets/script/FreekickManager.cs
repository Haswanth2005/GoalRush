using System.Collections;
using UnityEngine;

/// <summary>
/// In FreekickPractice mode, monitors when the ball has come to rest
/// after a kick and tells GameManager to reset it.
/// </summary>
public class FreekickManager : MonoBehaviour
{
    [Tooltip("Speed threshold below which ball is considered 'stopped'.")]
    public float stopSpeedThreshold = 0.4f;
    [Tooltip("How long ball must be slow before we count it as stopped.")]
    public float stopDuration = 1.5f;

    private float _slowTimer = 0f;
    private bool  _waitingForKick = false;   // true after ball has been kicked

    // ─────────────────────────────────────────────
    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStarted += OnGameStarted;
            GameManager.Instance.OnBallReset   += OnBallReset;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStarted -= OnGameStarted;
            GameManager.Instance.OnBallReset   -= OnBallReset;
        }
    }

    private void OnGameStarted()
    {
        _waitingForKick = false;
        _slowTimer = 0f;
    }

    private void OnBallReset()
    {
        _waitingForKick = false;
        _slowTimer = 0f;
    }

    // ─────────────────────────────────────────────
    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.currentMode  != GameMode.FreekickPractice) return;
        if (GameManager.Instance.currentState != GameState.Playing) return;
        if (Ball.Instance == null) return;

        float speed = Ball.Instance.rb.linearVelocity.magnitude;

        // Wait for player to kick (ball must first be moving fast)
        if (!_waitingForKick)
        {
            if (speed > 2f) _waitingForKick = true;
            return;
        }

        // Count time ball is moving slowly
        if (speed < stopSpeedThreshold)
        {
            _slowTimer += Time.deltaTime;
            if (_slowTimer >= stopDuration)
            {
                _waitingForKick = false;
                _slowTimer = 0f;
                GameManager.Instance.FreekickReset();
            }
        }
        else
        {
            _slowTimer = 0f;
        }
    }
}
