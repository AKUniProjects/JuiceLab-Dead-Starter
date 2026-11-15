using UnityEngine;

public sealed class BrickImpactFX : MonoBehaviour
{
    [SerializeField] private ParticleSystem colorfulSparks;
    [SerializeField] private ParticleSystem brickFragments;

    public bool HasParticles(int sparks, int fragments) =>
        (sparks > 0 && colorfulSparks != null) || (fragments > 0 && brickFragments != null);

    public void Play(int sparks, int fragments, Color brickColor, float duration)
    {
        PlayBurst(colorfulSparks, sparks, duration);
        PlayBurst(brickFragments, fragments, duration, brickColor);
        Destroy(gameObject, duration);
    }

    private static void PlayBurst(ParticleSystem particles, int count, float duration, Color? tint = null)
    {
        if (particles == null || count <= 0) return;

        var main = particles.main;
        main.simulationSpeed *= main.duration / duration;
        main.maxParticles = Mathf.Max(main.maxParticles, count);
        if (tint.HasValue) main.startColor = tint.Value;

        var emission = particles.emission;
        var burst = emission.GetBurst(0);
        if (burst.count.constant != count)
        {
            burst.count = new ParticleSystem.MinMaxCurve(count);
            emission.SetBurst(0, burst);
        }

        particles.Play(false);
    }
}
