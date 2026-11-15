using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallTrailFeedback : MonoBehaviour
{
    [SerializeField] private Rigidbody2D ballBody;
    [SerializeField] private TrailRenderer trail;

    [SerializeField] private bool enableBallTrail = true;
    [SerializeField] private Color trailColor = new Color32(69, 230, 245, 255);
    [SerializeField, Min(0.01f)] private float trailTime = 0.15f;
    [SerializeField, Min(0f)] private float trailStartWidth = 0.32f;
    [SerializeField, Min(0f)] private float trailEndWidth;
    [SerializeField, Range(0f, 1f)] private float trailOpacity = 0.85f;

    private Vector2 lastVisualPosition;
    private Vector2 lastBodyPosition;
    private int blockedFrame = -1;
    private bool settingsChanged;

    private void OnEnable()
    {
        if (ballBody == null || trail == null) return;

        ApplySettings();
        lastVisualPosition = trail.transform.position;
        lastBodyPosition = ballBody.position;
        ClearTrail();
        GameEvents.BallLost += OnBallLost;
    }

    private void OnDisable()
    {
        GameEvents.BallLost -= OnBallLost;
        if (trail != null) ClearTrail();
    }

    private void OnValidate() => settingsChanged = true;

    private void OnBallLost()
    {
        blockedFrame = Time.frameCount;
        ClearTrail();
    }

    private void LateUpdate()
    {
        if (ballBody == null || trail == null) return;
        if (settingsChanged) ApplySettings();

        Vector2 bodyPosition = ballBody.position;
        Vector2 visualPosition = trail.transform.position;
        float speed = ballBody.linearVelocity.magnitude;
        // Allow for this frame's movement and one physics step of interpolation.
        float travelLimit = 0.4f + speed * (Time.deltaTime + Time.fixedDeltaTime);
        bool jumped = (bodyPosition - lastBodyPosition).sqrMagnitude > travelLimit * travelLimit ||
                      (visualPosition - lastVisualPosition).sqrMagnitude > travelLimit * travelLimit;
        bool moving = enableBallTrail && Time.timeScale > 0f && ballBody.simulated && speed > 0.01f;
        if (jumped) blockedFrame = Time.frameCount + 1;
        if (!moving || Time.frameCount <= blockedFrame)
            ClearTrail();
        else
        {
            if (!trail.enabled)
            {
                trail.enabled = true;
                trail.Clear();
            }
            trail.emitting = true;
        }

        lastBodyPosition = bodyPosition;
        lastVisualPosition = visualPosition;
    }

    private void ApplySettings()
    {
        settingsChanged = false;
        trail.time = trailTime;
        trail.widthCurve = AnimationCurve.EaseInOut(0f, trailStartWidth, 1f, trailEndWidth);
        Color head = trailColor;
        head.a *= trailOpacity;
        Color tail = head;
        tail.a = 0f;
        trail.startColor = head;
        trail.endColor = tail;
    }

    private void ClearTrail()
    {
        if (!trail.enabled) return;

        trail.enabled = false;

        trail.emitting = false;
        trail.Clear();
    }
}