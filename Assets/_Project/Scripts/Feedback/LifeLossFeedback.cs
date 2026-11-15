using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LifeLossFeedback : MonoBehaviour
{
    [SerializeField] private Image flashOverlay;
    [SerializeField] private bool enableLifeLossFlash = true;
    [SerializeField] private Color flashColor = new Color32(255, 69, 69, 255);
    [SerializeField, Min(0.01f)] private float flashDuration = 0.18f;
    [SerializeField, Range(0f, 1f)] private float maximumAlpha = 0.25f;

    private float flashStarted;
    private bool flashing;

    private void OnEnable()
    {
        if (flashOverlay == null) return;

        flashOverlay.raycastTarget = false;
        ClearFlash();
        GameEvents.BallLost += OnBallLost;
    }

    private void OnDisable()
    {
        GameEvents.BallLost -= OnBallLost;
        if (flashOverlay != null) ClearFlash();
    }

    private void OnBallLost()
    {
        if (!enableLifeLossFlash || maximumAlpha <= 0f) return;

        flashing = true;
        flashStarted = Time.unscaledTime;
        SetAlpha(maximumAlpha);
        flashOverlay.enabled = true;
    }

    private void Update()
    {
        if (!flashing) return;

        float elapsed = Time.unscaledTime - flashStarted;
        if (!enableLifeLossFlash || elapsed >= flashDuration)
        {
            ClearFlash();
            return;
        }

        SetAlpha(maximumAlpha * Mathf.SmoothStep(0f, 1f, 1f - elapsed / flashDuration));
    }

    private void SetAlpha(float alpha)
    {
        Color color = flashColor;
        color.a = alpha;
        flashOverlay.color = color;
    }

    private void ClearFlash()
    {
        flashing = false;
        SetAlpha(0f);
        flashOverlay.enabled = false;
    }
}