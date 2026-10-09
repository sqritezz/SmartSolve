using UnityEngine;

// Put this on the SAME object as the Audio Source you want shorter.
// Whenever that sound plays, it is faded out and stopped after "Max Length" seconds.
// Works no matter which script plays the sound (warning, wrong slot, etc.).
[RequireComponent(typeof(AudioSource))]
public class SoundCutOff : MonoBehaviour
{
    [Tooltip("How many seconds of the sound to keep")]
    public float maxLength = 0.6f;
    [Tooltip("Short fade at the end so it doesn't click (seconds)")]
    public float fadeOut = 0.1f;
    [Tooltip("Optional: skip the start of the clip (seconds), e.g. silence at the beginning")]
    public float startAt = 0f;

    private AudioSource source;
    private float originalVolume;
    private float playTimer = -1f;
    private bool wasPlaying = false;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        originalVolume = source.volume;
    }

    private void Update()
    {
        bool playing = source.isPlaying;

        // Sound just started
        if (playing && !wasPlaying)
        {
            playTimer = 0f;
            source.volume = originalVolume;
            if (startAt > 0f && source.clip != null && startAt < source.clip.length)
                source.time = startAt;
        }

        if (playing && playTimer >= 0f)
        {
            playTimer += Time.unscaledDeltaTime;

            float fadeStart = Mathf.Max(0f, maxLength - fadeOut);
            if (playTimer >= fadeStart && fadeOut > 0f)
                source.volume = originalVolume * Mathf.Clamp01(1f - (playTimer - fadeStart) / fadeOut);

            if (playTimer >= maxLength)
            {
                source.Stop();
                source.volume = originalVolume;
                playTimer = -1f;
                playing = false;
            }
        }

        if (!playing) playTimer = -1f;
        wasPlaying = playing;
    }
}