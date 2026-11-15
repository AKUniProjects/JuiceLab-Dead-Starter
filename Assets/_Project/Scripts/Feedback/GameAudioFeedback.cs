using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameAudioFeedback : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private bool enablePaddleHitSound = true;
    [SerializeField] private bool enableBrickDestructionSound = true;
    [SerializeField] private bool enableBallLossSound = true;
    [SerializeField] private AudioClip paddleHit;
    [SerializeField] private AudioClip brickBreak;
    [SerializeField] private AudioClip ballLost;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 0.70f;
    [SerializeField, Range(0f, 1f)] private float paddleHitVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float brickBreakVolume = 0.65f;
    [SerializeField, Range(0f, 1f)] private float ballLossVolume = 0.70f;

    [Header("Duration (%)")]
    [SerializeField, Range(0f, 100f), InspectorName("Paddle Hit Duration")]
    private float paddleHitDurationPercent = 100f;
    [SerializeField, Range(0f, 100f), InspectorName("Brick Break Duration")]
    private float brickBreakDurationPercent = 25f;
    [SerializeField, Range(0f, 100f), InspectorName("Ball Loss Duration")]
    private float ballLossDurationPercent = 100f;

    [Header("Game Over Sound")]
    [SerializeField] private bool enableGameOverSound = true;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField, Range(0f, 1f)] private float gameOverVolume = 0.70f;
    [SerializeField, Range(0f, 100f), InspectorName("Game Over Duration (%)")]
    private float gameOverDurationPercent = 100f;
    [SerializeField] private GameFlowUI gameFlow;

    private const int BrickVoiceCount = 3;
    private const float FadeOutSeconds = 0.02f;
    private Voice paddleVoice;
    private Voice ballLossVoice;
    private Voice gameOverVoice;
    private Voice[] brickVoices;

    private void Awake()
    {
        paddleVoice = CreateVoice(paddleHit);
        ballLossVoice = CreateVoice(ballLost);
        gameOverVoice = CreateVoice(gameOverClip);
        brickVoices = new Voice[BrickVoiceCount];
        for (int i = 0; i < brickVoices.Length; i++)
            brickVoices[i] = CreateVoice(brickBreak);

        // These imports defer audio data loading. Load once before the first hit.
        if (paddleHit != null) paddleHit.LoadAudioData();
        if (brickBreak != null) brickBreak.LoadAudioData();
        if (ballLost != null) ballLost.LoadAudioData();
        if (gameOverClip != null) gameOverClip.LoadAudioData();
    }

    private void OnEnable()
    {
        GameEvents.BallHitPaddle += OnBallHitPaddle;
        GameEvents.BrickDestroyed += OnBrickDestroyed;
        GameEvents.BallLost += OnBallLost;
        if (gameFlow != null)
        {
            gameFlow.GameOverShown += OnGameOverShown;
            gameFlow.GameOverDismissed += StopGameOver;
        }
    }

    private void OnDisable()
    {
        GameEvents.BallHitPaddle -= OnBallHitPaddle;
        GameEvents.BrickDestroyed -= OnBrickDestroyed;
        GameEvents.BallLost -= OnBallLost;
        if (gameFlow != null)
        {
            gameFlow.GameOverShown -= OnGameOverShown;
            gameFlow.GameOverDismissed -= StopGameOver;
        }

        paddleVoice.Stop();
        ballLossVoice.Stop();
        gameOverVoice.Stop();
        foreach (Voice voice in brickVoices)
            voice.Stop();
    }

    private Voice CreateVoice(AudioClip clip)
    {
        AudioSource voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 0f;
        voice.spatialize = false;
        voice.clip = clip;
        return new Voice(voice);
    }

    private void OnBallHitPaddle(ImpactInfo impact)
    {
        if (enablePaddleHitSound) paddleVoice.Play(masterVolume * paddleHitVolume, paddleHitDurationPercent);
    }

    private void OnBallLost()
    {
        if (gameFlow != null && gameFlow.ReplacesBallLossSound) return;
        if (enableBallLossSound) ballLossVoice.Play(masterVolume * ballLossVolume, ballLossDurationPercent);
    }

    private void OnGameOverShown()
    {
        ballLossVoice.Stop();
        if (enableGameOverSound) gameOverVoice.Play(masterVolume * gameOverVolume, gameOverDurationPercent);
    }

    private void StopGameOver() => gameOverVoice.Stop();

    private void OnBrickDestroyed(ImpactInfo impact)
    {
        float gain = masterVolume * brickBreakVolume;
        if (!enableBrickDestructionSound || brickBreak == null || gain <= 0f || brickBreakDurationPercent <= 0f) return;

        // Reuse the oldest voice when all three are busy and share the gain across the burst.
        Voice next = brickVoices[0];
        foreach (Voice voice in brickVoices)
        {
            if (!voice.IsPlaying)
            {
                next = voice;
                break;
            }

            if (voice.StartTime < next.StartTime) next = voice;
        }

        int activeCount = 1;
        foreach (Voice voice in brickVoices)
            if (voice != next && voice.IsPlaying) activeCount++;

        float voiceVolume = gain / activeCount;
        foreach (Voice voice in brickVoices)
            if (voice != next && voice.IsPlaying) voice.SetGain(voiceVolume);

        next.Play(voiceVolume, brickBreakDurationPercent);
    }

    private void Update()
    {
        double now = AudioSettings.dspTime;
        paddleVoice.UpdateFade(now);
        ballLossVoice.UpdateFade(now);
        gameOverVoice.UpdateFade(now);
        foreach (Voice voice in brickVoices)
            voice.UpdateFade(now);
    }

    internal sealed class Voice
    {
        private readonly AudioSource source;
        private double endTime;
        private float fadeDuration;
        private float gain;
        private float fadeGain;

        public Voice(AudioSource source) => this.source = source;

        public bool IsPlaying => source.isPlaying;
        public double StartTime { get; private set; }

        public void Play(float volume, float percentage)
        {
            if (source.clip == null || volume <= 0f) return;

            float clipLength = source.clip.length;
            float duration = clipLength * Mathf.Clamp01(percentage / 100f);
            if (duration <= 0f) return;

            source.Stop();
            fadeGain = 1f;
            SetGain(volume);

            StartTime = AudioSettings.dspTime;
            endTime = StartTime + duration;
            fadeDuration = duration < clipLength ? Mathf.Min(FadeOutSeconds, duration) : 0f;
            source.PlayScheduled(StartTime);
            if (fadeDuration > 0f)
                source.SetScheduledEndTime(endTime);
        }

        public void SetGain(float volume)
        {
            gain = volume;
            source.volume = gain * fadeGain;
        }

        public void UpdateFade(double now)
        {
            if (fadeDuration <= 0f || !IsPlaying || now < endTime - fadeDuration) return;

            fadeGain = Mathf.Clamp01((float)(endTime - now) / fadeDuration);
            source.volume = gain * fadeGain;
        }

        public void Stop() => source.Stop();
    }
}
