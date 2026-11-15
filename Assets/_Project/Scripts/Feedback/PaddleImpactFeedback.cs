using UnityEngine;

[DisallowMultipleComponent]
public sealed class PaddleImpactFeedback : MonoBehaviour
{
    [SerializeField] private SpriteRenderer paddleVisual;

    [Header("Squash & Stretch")]
    [SerializeField, InspectorName("Enable Squash & Stretch")] private bool enableSquashStretch = true;
    [SerializeField, Range(0.1f, 1f)] private float squashScaleY = 0.80f;
    [SerializeField, Min(1f)] private float stretchScaleX = 1.12f;
    [SerializeField, Min(0.01f)] private float squashDuration = 0.22f;

    [Header("Paddle Recoil")]
    [SerializeField] private bool enablePaddleRecoil = true;
    [SerializeField, Min(0f)] private float recoilDistance = 0.22f;
    [SerializeField, Min(0.01f)] private float recoilDuration = 0.24f;

    [Header("Impact Flash")]
    [SerializeField] private bool enableImpactFlash = true;
    [SerializeField] private Color flashColor = new Color(0.38f, 0.92f, 1f);
    [SerializeField, Min(0.01f)] private float flashDuration = 0.17f;

    private Transform visual;
    private Vector3 restScale;
    private Vector3 restPosition;
    private Color restColor;
    private Vector3 localRecoil;
    private float elapsed;
    private bool animating;
    private Vector3 weights;
    private Vector3 impactStartWeights;

    private void OnEnable()
    {
        if (paddleVisual == null || paddleVisual.transform == transform ||
            !paddleVisual.transform.IsChildOf(transform)) return;

        visual = paddleVisual.transform;
        restScale = visual.localScale;
        restPosition = visual.localPosition;
        restColor = paddleVisual.color;
        GameEvents.BallHitPaddle += OnBallHitPaddle;
    }

    private void OnDisable()
    {
        GameEvents.BallHitPaddle -= OnBallHitPaddle;
        if (animating && visual != null && paddleVisual != null)
        {
            visual.localScale = restScale;
            visual.localPosition = restPosition;
            paddleVisual.color = restColor;
        }

        animating = false;
        weights = Vector3.zero;
    }

    private void OnBallHitPaddle(ImpactInfo impact)
    {
        if (!enableSquashStretch && !enablePaddleRecoil && !enableImpactFlash) return;

        // The parent scales Y by 0.5 in After. Convert the world-space distance to local space.
        localRecoil = visual.parent.InverseTransformVector(Vector3.down * recoilDistance);
        impactStartWeights = weights;
        elapsed = 0f;
        animating = true;
    }

    private void LateUpdate()
    {
        if (!animating) return;

        elapsed += Time.deltaTime;
        float squash = enableSquashStretch ? Pulse(squashDuration, impactStartWeights.x, spring: true) : 0f;
        float recoil = enablePaddleRecoil ? Pulse(recoilDuration, impactStartWeights.y) : 0f;
        float flash = enableImpactFlash ? Pulse(flashDuration, impactStartWeights.z) : 0f;

        if (squash != 0f || weights.x != 0f)
        {
            visual.localScale = new Vector3(
                restScale.x * Mathf.LerpUnclamped(1f, stretchScaleX, squash),
                restScale.y * Mathf.LerpUnclamped(1f, squashScaleY, squash),
                restScale.z);
        }

        if (recoil != 0f || weights.y != 0f)
            visual.localPosition = restPosition + localRecoil * recoil;

        if (flash != 0f || weights.z != 0f)
        {
            Color tint = Color.Lerp(restColor, flashColor, flash);
            tint.a = restColor.a;
            paddleVisual.color = tint;
        }

        weights = new Vector3(squash, recoil, flash);
        animating = (enableSquashStretch && elapsed < squashDuration)
                 || (enablePaddleRecoil && elapsed < recoilDuration)
                 || (enableImpactFlash && elapsed < flashDuration);
    }

    private float Pulse(float duration, float startWeight, bool spring = false)
    {
        if (elapsed >= duration) return 0f;

        float attack = duration * 0.18f;
        if (elapsed < attack) return Mathf.SmoothStep(startWeight, 1f, elapsed / attack);

        float recovery = (elapsed - attack) / (duration - attack);
        float tail = 1f - recovery;
        // Squash travels a little past rest during recovery before settling.
        float recoveryWeight = tail * tail * (1f + 2f * recovery);
        return spring ? recoveryWeight - 4f * recovery * recovery * tail * tail : recoveryWeight;
    }
}
