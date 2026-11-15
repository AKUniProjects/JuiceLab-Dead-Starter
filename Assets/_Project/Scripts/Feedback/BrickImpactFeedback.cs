using UnityEngine;

[DisallowMultipleComponent]
public sealed class BrickImpactFeedback : MonoBehaviour
{
    [SerializeField] private BrickImpactFX impactPrefab;

    [Header("Brick Impact Feedback")]
    [SerializeField] private bool enableColorfulSparks = true;
    [SerializeField] private bool enableBrickFragments = true;
    [SerializeField, Min(0)] private int sparksCount = 15;
    [SerializeField, Min(0)] private int fragmentsCount = 8;
    [SerializeField, Min(0.01f)] private float effectDuration = 0.5f;

    private void OnEnable() => GameEvents.BrickDestroyed += OnBrickDestroyed;
    private void OnDisable() => GameEvents.BrickDestroyed -= OnBrickDestroyed;

    private void OnBrickDestroyed(ImpactInfo impact)
    {
        int sparks = enableColorfulSparks ? sparksCount : 0;
        int fragments = enableBrickFragments ? fragmentsCount : 0;
        if (impactPrefab == null || !impactPrefab.HasParticles(sparks, fragments)) return;

        BrickImpactFX effect = Instantiate(impactPrefab, impact.Point, Quaternion.identity, transform);
        effect.Play(sparks, fragments, impact.Color, Mathf.Max(0.01f, effectDuration));
    }
}
