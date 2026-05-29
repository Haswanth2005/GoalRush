using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameMode  { NormalGame, FreekickPractice }
public enum GameState { MainMenu, Playing, GoalScored, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int goalTarget = 3;

    // ---- runtime state ----
    [HideInInspector] public GameMode  currentMode;
    [HideInInspector] public GameState currentState = GameState.MainMenu;
    [HideInInspector] public int playerScore = 0;
    [HideInInspector] public int aiScore    = 0;

    // ---- events ----
    public event Action<bool> OnGoalScored;   // true = player scored
    public event Action       OnGameOver;
    public event Action       OnGameStarted;
    public event Action       OnBallReset;
    public event Action       OnMainMenu;

    // ---- internal ----
    private Vector3 _ballSpawnPos;
    private Quaternion _ballSpawnRot;

    // ─────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Cache ball spawn
        if (Ball.Instance != null)
        {
            _ballSpawnPos = Ball.Instance.transform.position;
            _ballSpawnRot = Ball.Instance.transform.rotation;
        }
        // Start paused on main menu
        Time.timeScale = 0f;
        currentState = GameState.MainMenu;
    }

    // ─────────────────────────────────────────────
    public void StartGame(GameMode mode)
    {
        currentMode  = mode;
        playerScore  = 0;
        aiScore      = 0;
        currentState = GameState.Playing;
        Time.timeScale = 1f;

        ResetBallImmediate();
        OnGameStarted?.Invoke();
    }

    // ─────────────────────────────────────────────
    /// <summary>Called by GoalTrigger when the ball enters a goal zone.</summary>
    public void GoalScored(bool playerTeamScored)
    {
        if (currentState != GameState.Playing) return;

        if (playerTeamScored) playerScore++;
        else                  aiScore++;

        currentState = GameState.GoalScored;
        OnGoalScored?.Invoke(playerTeamScored);

        StartCoroutine(AfterGoalRoutine());
    }

    private IEnumerator AfterGoalRoutine()
    {
        // Give a short celebration pause (real time so UI still works if timeScale 0)
        yield return new WaitForSecondsRealtime(2.5f);

        if (playerScore >= goalTarget || aiScore >= goalTarget)
        {
            currentState = GameState.GameOver;
            Time.timeScale = 0f;
            OnGameOver?.Invoke();
        }
        else
        {
            currentState = GameState.Playing;
            ResetBallImmediate();
            OnBallReset?.Invoke();
        }
    }

    // ─────────────────────────────────────────────
    /// <summary>Called by FreekickManager after each kick lands.</summary>
    public void FreekickReset()
    {
        if (currentMode != GameMode.FreekickPractice) return;
        if (currentState != GameState.Playing) return;
        StartCoroutine(DelayedFreekickReset());
    }

    private IEnumerator DelayedFreekickReset()
    {
        yield return new WaitForSeconds(3f);   // let the ball roll / settle
        ResetBallImmediate();
        OnBallReset?.Invoke();
    }

    // ─────────────────────────────────────────────
    public void GoToMainMenu()
    {
        StopAllCoroutines();
        currentState = GameState.MainMenu;
        playerScore  = 0;
        aiScore      = 0;
        Time.timeScale = 0f;
        ResetBallImmediate();
        OnMainMenu?.Invoke();
    }

    public void RestartGame()
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ─────────────────────────────────────────────
    private void ResetBallImmediate()
    {
        if (Ball.Instance == null) return;
        Ball.Instance.rb.linearVelocity  = Vector3.zero;
        Ball.Instance.rb.angularVelocity = Vector3.zero;
        Ball.Instance.transform.SetPositionAndRotation(_ballSpawnPos, _ballSpawnRot);
    }
}
