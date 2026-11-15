using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class BallCollisionPulseFeedback : MonoBehaviour
{
    [SerializeField] private Transform wallRoot;
    [SerializeField] private Material pulseMaterial;

    [SerializeField] private bool enableCollisionPulse = true;
    [SerializeField] private Color pulseColor = new Color32(69, 230, 245, 255);
    [SerializeField, Min(0.01f)] private float pulseDuration = 0.22f;
    [SerializeField, Min(0f)] private float initialDiameter = 0.30f;
    [SerializeField, Min(0f)] private float finalDiameter = 0.95f;
    [SerializeField, Range(0f, 1f)] private float maximumOpacity = 0.85f;
    [SerializeField, Min(0.01f)] private float ringThickness = 0.075f;
    [SerializeField] private bool enableWallPulses = true;
    [SerializeField] private bool enablePaddlePulses;
    [SerializeField] private bool enableBrickPulses;

    private const int PoolSize = 8;
    private const int Segments = 48;
    private readonly LineRenderer[] rings = new LineRenderer[PoolSize];
    private readonly float[] ages = new float[PoolSize];
    private int nextRing;
    private int lossFrame = -1;

    private void Awake()
    {
        if (pulseMaterial == null) return;

        // Keep world-space impacts stationary while the ball moves away.
        var pool = new GameObject("Ball Collision Pulse Pool");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pool, gameObject.scene);
        var circle = new Vector3[Segments];
        for (int i = 0; i < circle.Length; i++)
        {
            float angle = 2f * Mathf.PI * i / circle.Length;
            circle[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.5f;
        }

        for (int i = 0; i < rings.Length; i++)
        {
            var ring = new GameObject("Collision Pulse").AddComponent<LineRenderer>();
            ring.transform.SetParent(pool.transform, false);
            ring.sharedMaterial = pulseMaterial;
            ring.loop = true;
            ring.useWorldSpace = false;
            ring.textureMode = LineTextureMode.Stretch;
            ring.sortingOrder = 1;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.positionCount = circle.Length;
            ring.SetPositions(circle);
            ring.enabled = false;
            rings[i] = ring;
        }
    }

    private void OnEnable() => GameEvents.BallLost += OnBallLost;

    private void OnDisable()
    {
        GameEvents.BallLost -= OnBallLost;
        Clear();
    }

    private void OnDestroy()
    {
        if (rings[0] != null) Destroy(rings[0].transform.parent.gameObject);
    }

    private void OnBallLost()
    {
        lossFrame = Time.frameCount;
        Clear();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActiveAndEnabled || !enableCollisionPulse || Time.timeScale <= 0f ||
            lossFrame == Time.frameCount || collision.contactCount == 0 || rings[0] == null) return;

        var target = collision.collider.transform;
        bool eligible = (enableWallPulses && wallRoot != null && target.IsChildOf(wallRoot)) ||
                        (enablePaddlePulses && target.GetComponentInParent<Paddle>() != null) ||
                        (enableBrickPulses && target.GetComponentInParent<Brick>() != null);
        if (!eligible) return;

        int index = nextRing;
        nextRing = (nextRing + 1) % rings.Length;
        LineRenderer ring = rings[index];
        ring.transform.position = collision.GetContact(0).point;
        ages[index] = 0f;
        Draw(ring, 0f);
        ring.enabled = true;
    }

    private void LateUpdate()
    {
        if (!enableCollisionPulse || Time.timeScale <= 0f)
        {
            Clear();
            return;
        }

        for (int i = 0; i < rings.Length; i++)
        {
            LineRenderer ring = rings[i];
            if (ring == null || !ring.enabled) continue;

            ages[i] += Time.deltaTime;
            if (ages[i] >= pulseDuration)
                ring.enabled = false;
            else
                Draw(ring, ages[i] / pulseDuration);
        }
    }

    private void Draw(LineRenderer ring, float progress)
    {
        float expansion = 1f - (1f - progress) * (1f - progress);
        ring.transform.localScale = Vector3.one * Mathf.Lerp(initialDiameter, finalDiameter, expansion);
        ring.widthMultiplier = ringThickness * Mathf.Lerp(1f, 0.35f, progress);
        Color tint = pulseColor;
        tint.a *= maximumOpacity * (1f - Mathf.SmoothStep(0f, 1f, progress));
        ring.startColor = ring.endColor = tint;
    }

    private void Clear()
    {
        foreach (LineRenderer ring in rings)
            if (ring != null) ring.enabled = false;
    }
}