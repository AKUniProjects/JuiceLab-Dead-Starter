using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameFlowUI : MonoBehaviour
{
    [Header("Start Screen")]
    [SerializeField] private bool enableStartScreen = true;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private Button playButton;

    [Header("Game Over Screen")]
    [SerializeField] private bool enableGameOverScreen = true;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private TMP_Text finalScoreText;

    [Header("Victory Screen")]
    [SerializeField] private bool enableVictoryScreen = true;
    [SerializeField] private LevelBuilder levelBuilder;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Button victoryPlayAgainButton;
    [SerializeField] private TMP_Text victoryScoreText;
    [SerializeField] private VictoryFeedback victoryFeedback;

    private Coroutine victoryCheck;

    private enum Screen { None, Start, GameOver, Victory, Restarting }
    private static string restartingScene;
    private Screen screen;
    private int lastLives;
    private int lastScore;
    private int scoreBeforeReset;
    private bool lostFinalLife;
    private bool ownsPause;
    private float previousTimeScale;

    internal event Action GameOverShown;
    internal event Action GameOverDismissed;

    // BallLost subscribers can run on either side of the Game Over transition.
    internal bool ReplacesBallLossSound =>
        isActiveAndEnabled && (screen == Screen.GameOver || CanShowGameOver);

    private bool CanShowGameOver => enableGameOverScreen && lostFinalLife && screen == Screen.None;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRestart() => restartingScene = null;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            lastLives = GameManager.Instance.Lives;
            lastScore = GameManager.Instance.Score;
        }
        GameEvents.ScoreChanged += OnScoreChanged;
        GameEvents.LivesChanged += OnLivesChanged;
        GameEvents.BallLost += OnBallLost;
        playButton.onClick.AddListener(Play);
        playAgainButton.onClick.AddListener(PlayAgain);
        if (victoryPlayAgainButton != null) victoryPlayAgainButton.onClick.AddListener(PlayAgain);
        GameEvents.BrickDestroyed += OnBrickDestroyed;
    }

    private void Start()
    {
        lastLives = GameManager.Instance.Lives;
        lastScore = GameManager.Instance.Score;
        bool restarting = restartingScene == gameObject.scene.path;
        if (restarting) restartingScene = null;
        if (enableStartScreen && !restarting)
        {
            screen = Screen.Start;
            Pause();
            startPanel.SetActive(true);
            playButton.Select();
        }
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= OnScoreChanged;
        GameEvents.LivesChanged -= OnLivesChanged;
        GameEvents.BallLost -= OnBallLost;
        playButton.onClick.RemoveListener(Play);
        playAgainButton.onClick.RemoveListener(PlayAgain);
        if (victoryPlayAgainButton != null) victoryPlayAgainButton.onClick.RemoveListener(PlayAgain);
        GameEvents.BrickDestroyed -= OnBrickDestroyed;
        if (victoryCheck != null) StopCoroutine(victoryCheck);
        victoryCheck = null;
        if (victoryPanel != null) victoryPanel.SetActive(false);
        startPanel.SetActive(false);
        if (screen == Screen.GameOver) GameOverDismissed?.Invoke();
        gameOverPanel.SetActive(false);
        screen = Screen.None;
        lostFinalLife = false;
        Resume();
    }

    private void OnScoreChanged(int score)
    {
        // The starter publishes the reset while the previous lives count is still one.
        if (lastLives == 1 && score == 0) scoreBeforeReset = lastScore;
        lastScore = score;
    }

    private void OnLivesChanged(int lives)
    {
        lostFinalLife = lastLives == 1;
        lastLives = lives;
    }

    private void OnBallLost()
    {
        bool showGameOver = CanShowGameOver;
        lostFinalLife = false;
        if (!showGameOver) return;

        screen = Screen.GameOver;
        finalScoreText.text = $"Final Score  {scoreBeforeReset}";
        Pause();
        gameOverPanel.SetActive(true);
        playAgainButton.Select();
        GameOverShown?.Invoke();
    }

    private void OnBrickDestroyed(ImpactInfo impact)
    {
        if (enableVictoryScreen && levelBuilder != null && victoryPanel != null && screen == Screen.None && victoryCheck == null)
            victoryCheck = StartCoroutine(CheckForVictory());
    }

    private IEnumerator CheckForVictory()
    {
        // BrickDestroyed fires before the brick queues its destruction.
        yield return null;
        victoryCheck = null;
        if (screen != Screen.None || levelBuilder.GetComponentInChildren<Brick>() != null) yield break;

        screen = Screen.Victory;
        if (victoryScoreText != null) victoryScoreText.text = $"Final Score  {lastScore}";
        Pause();
        victoryPanel.SetActive(true);
        if (victoryPlayAgainButton != null) victoryPlayAgainButton.Select();
        if (victoryFeedback != null && victoryFeedback.isActiveAndEnabled) victoryFeedback.Celebrate();
    }

    private void Play()
    {
        if (screen != Screen.Start) return;

        screen = Screen.None;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        startPanel.SetActive(false);
        Resume();
    }

    private void PlayAgain()
    {
        if (screen != Screen.GameOver && screen != Screen.Victory) return;

        if (screen == Screen.GameOver) GameOverDismissed?.Invoke();
        screen = Screen.Restarting;
        playAgainButton.interactable = false;
        if (victoryPlayAgainButton != null) victoryPlayAgainButton.interactable = false;
        Scene scene = gameObject.scene;
        restartingScene = scene.path;
#if UNITY_EDITOR
        // After can run directly from the Editor even when omitted from the build profile.
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
            scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadSceneAsync(scene.buildIndex, LoadSceneMode.Single);
#endif
    }

    private void Pause()
    {
        if (ownsPause) return;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        ownsPause = true;
    }

    private void Resume()
    {
        if (!ownsPause) return;
        Time.timeScale = previousTimeScale;
        ownsPause = false;
    }
}