using TMPro;
using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScoreFeedback : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text floatingPointsText;

    [Header("Score Appearance")]
    [SerializeField] private Color baseScoreColor = Color.white;
    [SerializeField, Min(1f)] private float scoreTextSize = 48f;
    [SerializeField] private bool scoreDisplayPreview;
    [SerializeField, Min(0)] private int previewValue;

    [Header("Score Pop")]
    [SerializeField] private bool enableScorePop = true;
    [SerializeField, Min(100f), InspectorName("Pop Scale (%)")] private float popScalePercent = 130f;
    [SerializeField, Min(0.01f)] private float popDuration = 0.22f;

    [Header("Score Flash")]
    [SerializeField] private bool enableScoreFlash = true;
    [SerializeField] private Color flashColor = new Color32(255, 215, 0, 255);
    [SerializeField, Min(0.01f)] private float flashDuration = 0.25f;

    [Header("Floating Points")]
    [SerializeField] private bool enableFloatingPoints = true;
    [SerializeField] private Color floatingPointsColor = new Color32(255, 215, 0, 255);
    [SerializeField, Min(0f)] private float floatingDistance = 60f;
    [SerializeField, Min(1f)] private float floatingSize = 42f;
    [SerializeField, Min(0.01f)] private float floatingDuration = 0.50f;

    private int currentVolleyPoints;
    private Vector2 floatingOrigin;
    private float floatingAge;
    private Vector3 normalScale;
    private int previousScore;
    private float popRemaining;
    private float flashRemaining;

    private void OnEnable()
    {
        if (scoreText == null) return;
        normalScale = scoreText.rectTransform.localScale;
        if (!Application.IsPlaying(gameObject)) return;

        previousScore = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        if (floatingPointsText != null) floatingOrigin = floatingPointsText.rectTransform.anchoredPosition;
        ResetVolley();
        GameEvents.ScoreChanged += OnScoreChanged;
        GameEvents.BallHitPaddle += OnBallHitPaddle;
        GameEvents.BallLost += ResetVolley;
        ApplyAppearance(0f, 0f);
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= OnScoreChanged;
        GameEvents.BallHitPaddle -= OnBallHitPaddle;
        GameEvents.BallLost -= ResetVolley;
        if (!Application.IsPlaying(gameObject)) return;

        ClearEffects();
    }

    private void OnScoreChanged(int score)
    {
        int gained = score - previousScore;
        previousScore = score;
        if (gained < 0 || score == 0)
        {
            ClearEffects();
            return;
        }

        if (gained == 0) return;

        currentVolleyPoints += gained;
        popRemaining = enableScorePop ? popDuration : 0f;
        flashRemaining = enableScoreFlash ? flashDuration : 0f;
        ApplyAppearance(popRemaining > 0f ? 1f : 0f, flashRemaining > 0f ? 1f : 0f);
        if (enableFloatingPoints && floatingPointsText != null)
        {
            floatingAge = 0f;
            floatingPointsText.text = $"+{currentVolleyPoints}";
            floatingPointsText.fontSize = floatingSize;
            floatingPointsText.color = floatingPointsColor;
            floatingPointsText.rectTransform.anchoredPosition = floatingOrigin;
            floatingPointsText.gameObject.SetActive(true);
        }
    }

    private void OnBallHitPaddle(ImpactInfo impact) => ResetVolley();

    private void Update()
    {
        if (scoreText == null) return;
        if (!Application.IsPlaying(gameObject))
        {
            ApplyAppearance(0f, 0f);
            scoreText.text = $"SCORE {(scoreDisplayPreview ? previewValue : 0)}";
            return;
        }

        popRemaining = enableScorePop ? Mathf.Max(0f, popRemaining - Time.deltaTime) : 0f;
        flashRemaining = enableScoreFlash ? Mathf.Max(0f, flashRemaining - Time.deltaTime) : 0f;
        ApplyAppearance(Weight(popRemaining, popDuration), Weight(flashRemaining, flashDuration));

        if (floatingPointsText == null || !floatingPointsText.gameObject.activeSelf) return;

        floatingAge += Time.deltaTime;
        if (!enableFloatingPoints || floatingAge >= floatingDuration)
        {
            HideFloatingPoints();
            return;
        }

        float progress = floatingAge / floatingDuration;
        float rise = Mathf.SmoothStep(0f, floatingDistance, progress);
        floatingPointsText.rectTransform.anchoredPosition = floatingOrigin + Vector2.up * rise;
        floatingPointsText.alpha = floatingPointsColor.a * (1f - progress);
    }

    private void ApplyAppearance(float pop, float flash)
    {
        scoreText.rectTransform.localScale = normalScale * Mathf.Lerp(1f, popScalePercent / 100f, pop);
        scoreText.color = Color.Lerp(baseScoreColor, flashColor, flash);
        scoreText.fontSize = scoreTextSize;
    }

    private void ClearEffects()
    {
        popRemaining = 0f;
        flashRemaining = 0f;
        if (scoreText != null) ApplyAppearance(0f, 0f);
        ResetVolley();
    }

    private void ResetVolley()
    {
        currentVolleyPoints = 0;
        HideFloatingPoints();
    }

    private void HideFloatingPoints()
    {
        if (floatingPointsText == null) return;

        floatingPointsText.gameObject.SetActive(false);
        floatingPointsText.rectTransform.anchoredPosition = floatingOrigin;
    }

    private static float Weight(float remaining, float duration) =>
        remaining > 0f ? Mathf.SmoothStep(0f, 1f, remaining / duration) : 0f;
}
