using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class VictoryFeedback : MonoBehaviour
{
    [Header("Victory Fireworks")]
    [SerializeField] private bool enableFireworks = true;
    [SerializeField, Range(1, 24)] private int fireworksCount = 8;
    [SerializeField, Range(0.25f, 2f)] private float fireworksIntensity = 1f;
    [SerializeField, Min(0.1f)] private float fireworksDuration = 2.2f;
    [SerializeField] private Material fireworksMaterial;

    [Header("Victory Audio")]
    [SerializeField] private bool enableVictorySound = true;
    [SerializeField] private AudioClip victoryClip;
    [SerializeField, Range(0f, 1f)] private float victoryVolume = 0.65f;
    [SerializeField, Range(0f, 100f), InspectorName("Victory Duration (%)")] private float victoryDurationPercent = 100f;
    [SerializeField] private bool enableFireworksSound = true;
    [SerializeField] private AudioClip fireworksClip;
    [SerializeField, Range(0f, 1f)] private float fireworksVolume = 0.35f;
    [SerializeField, Range(0f, 100f), InspectorName("Fireworks Duration (%)")] private float fireworksDurationPercent = 100f;

    private GameAudioFeedback.Voice victoryVoice;
    private GameAudioFeedback.Voice fireworksVoice;
    private ParticleSystem fireworks;
    private Coroutine sequence;

    private void Awake()
    {
        victoryVoice = CreateVoice(victoryClip);
        fireworksVoice = CreateVoice(fireworksClip);
    }

    public void Celebrate()
    {
        if (enableVictorySound) victoryVoice.Play(victoryVolume, victoryDurationPercent);
        if (enableFireworksSound) fireworksVoice.Play(fireworksVolume, fireworksDurationPercent);
        if (enableFireworks && fireworksMaterial != null && Camera.main != null && sequence == null)
            sequence = StartCoroutine(LaunchFireworks(Camera.main));
    }

    private void Update()
    {
        double now = AudioSettings.dspTime;
        victoryVoice.UpdateFade(now);
        fireworksVoice.UpdateFade(now);
    }

    private void OnDisable()
    {
        if (sequence != null) StopCoroutine(sequence);
        sequence = null;
        if (fireworks != null) fireworks.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        victoryVoice?.Stop();
        fireworksVoice?.Stop();
    }

    private GameAudioFeedback.Voice CreateVoice(AudioClip clip)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.clip = clip;
        if (clip != null) clip.LoadAudioData();
        return new GameAudioFeedback.Voice(source);
    }

    private IEnumerator LaunchFireworks(Camera camera)
    {
        if (fireworks == null) fireworks = CreateFireworks();

        fireworks.Play(false);

        float duration = Mathf.Max(0.1f, fireworksDuration);
        float lifetime = Mathf.Min(0.65f, duration);
        int count = Mathf.Max(1, fireworksCount);
        float start = Time.unscaledTime;
        for (int burst = 0; burst < count; burst++)
        {
            float delay = (duration - lifetime) * burst / Mathf.Max(1, count - 1);
            while (Time.unscaledTime - start < delay) yield return null;

            float x = (burst % 2 == 0 ? 0.23f : 0.77f) + Random.Range(-0.06f, 0.06f);
            var point = camera.ViewportToWorldPoint(new Vector3(x, Random.Range(0.36f, 0.77f), -camera.transform.position.z));
            Color color = Color.HSVToRGB(Random.value, 0.56f, 1f);
            int particles = Mathf.Clamp(Mathf.RoundToInt(26f * fireworksIntensity), 1, 65);
            float angleOffset = Random.value * 360f;
            for (int i = 0; i < particles; i++)
            {
                float angle = (angleOffset + 360f * i / particles) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                fireworks.Emit(new ParticleSystem.EmitParams
                {
                    position = point,
                    velocity = direction * Random.Range(1.5f, 4f) * Mathf.Sqrt(fireworksIntensity),
                    startColor = color,
                    startLifetime = lifetime * Random.Range(0.65f, 1f),
                    startSize = Random.Range(0.06f, 0.13f)
                }, 1);
            }
        }

        yield return new WaitForSecondsRealtime(lifetime);
        fireworks.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        sequence = null;
    }

    private ParticleSystem CreateFireworks()
    {
        var holder = new GameObject("Victory Particles");
        holder.transform.SetParent(transform, false);
        var system = holder.AddComponent<ParticleSystem>();
        system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.playOnAwake = false;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.25f;
        main.maxParticles = 1600;
        var emission = system.emission;
        emission.enabled = false;
        var shape = system.shape;
        shape.enabled = false;
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.45f), new GradientAlphaKey(0f, 1f) });
        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = fade;
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = fireworksMaterial;
        renderer.sortingOrder = 4;
        return system;
    }
}