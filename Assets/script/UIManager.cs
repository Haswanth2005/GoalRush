using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives all UI panels based on GameManager state events.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject hudPanel;
    public GameObject goalScoredPanel;
    public GameObject endGamePanel;

    [Header("Main Menu")]
    public Button normalGameButton;
    public Button freekickButton;

    [Header("HUD")]
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI aiScoreText;
    public TextMeshProUGUI modeLabel;
    public TextMeshProUGUI goalTargetText;

    [Header("Goal Scored")]
    public TextMeshProUGUI goalScoredText;   // "GOAL!" or "OWN GOAL!"

    [Header("End Game")]
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI finalScoreText;
    public Button restartButton;
    public Button mainMenuButton;

    // ─────────────────────────────────────────────
    private void Start()
    {
        // Wire buttons
        normalGameButton?.onClick.AddListener(() => GameManager.Instance.StartGame(GameMode.NormalGame));
        freekickButton  ?.onClick.AddListener(() => GameManager.Instance.StartGame(GameMode.FreekickPractice));
        restartButton   ?.onClick.AddListener(() => GameManager.Instance.RestartGame());
        mainMenuButton  ?.onClick.AddListener(() => GameManager.Instance.GoToMainMenu());

        // Subscribe to events
        GameManager.Instance.OnGameStarted += HandleGameStarted;
        GameManager.Instance.OnGoalScored  += HandleGoalScored;
        GameManager.Instance.OnGameOver    += HandleGameOver;
        GameManager.Instance.OnBallReset   += HandleBallReset;
        GameManager.Instance.OnMainMenu    += HandleMainMenu;

        // Initial state
        ShowPanel(mainMenuPanel);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnGameStarted -= HandleGameStarted;
        GameManager.Instance.OnGoalScored  -= HandleGoalScored;
        GameManager.Instance.OnGameOver    -= HandleGameOver;
        GameManager.Instance.OnBallReset   -= HandleBallReset;
        GameManager.Instance.OnMainMenu    -= HandleMainMenu;
    }

    // ─────────────────────────────────────────────
    private void Update()
    {
        // Live HUD score updates
        if (GameManager.Instance == null) return;
        if (hudPanel != null && hudPanel.activeSelf)
        {
            if (playerScoreText != null)
                playerScoreText.text = GameManager.Instance.playerScore.ToString();
            if (aiScoreText != null)
                aiScoreText.text = GameManager.Instance.aiScore.ToString();
        }
    }

    // ─────────────────────────────────────────────
    private void HandleGameStarted()
    {
        ShowPanel(hudPanel);

        // Update mode label
        if (modeLabel != null)
        {
            modeLabel.text = GameManager.Instance.currentMode == GameMode.FreekickPractice
                ? "⚽  FREE KICK"
                : "🏆  NORMAL GAME";
        }
        if (goalTargetText != null)
        {
            goalTargetText.text = GameManager.Instance.currentMode == GameMode.FreekickPractice
                ? ""
                : $"First to {GameManager.Instance.goalTarget}";
        }
    }

    private void HandleGoalScored(bool playerScored)
    {
        if (goalScoredPanel != null)
        {
            goalScoredPanel.SetActive(true);
            if (goalScoredText != null)
                goalScoredText.text = playerScored ? "GOAL! 🎉" : "OPPONENT SCORES! 😬";
        }
    }

    private void HandleBallReset()
    {
        if (goalScoredPanel != null)
            goalScoredPanel.SetActive(false);
    }

    private void HandleGameOver()
    {
        if (goalScoredPanel != null) goalScoredPanel.SetActive(false);
        ShowPanel(endGamePanel);

        if (winnerText != null)
        {
            int p = GameManager.Instance.playerScore;
            int a = GameManager.Instance.aiScore;
            if      (p > a) winnerText.text = "🏆 YOU WIN!";
            else if (a > p) winnerText.text = "YOU LOSE!";
            else            winnerText.text = "DRAW!";
        }
        if (finalScoreText != null)
        {
            finalScoreText.text = $"{GameManager.Instance.playerScore}  —  {GameManager.Instance.aiScore}";
        }
    }

    private void HandleMainMenu()
    {
        if (goalScoredPanel != null) goalScoredPanel.SetActive(false);
        ShowPanel(mainMenuPanel);
    }

    // ─────────────────────────────────────────────
    private void ShowPanel(GameObject target)
    {
        GameObject[] all = { mainMenuPanel, hudPanel, endGamePanel };
        foreach (var p in all)
            if (p != null) p.SetActive(false);

        if (target != null) target.SetActive(true);
    }
}
