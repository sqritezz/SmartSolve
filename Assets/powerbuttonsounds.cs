using UnityEngine;

/// <summary>
/// Plays the Windows startup / shutdown sounds for the Power Button.
/// Add this next to the Power Button script. No changes to powerbutton.cs needed:
/// it watches the "screenOn" object and plays a sound whenever it turns on or off.
/// </summary>
public class PowerButtonSounds : MonoBehaviour
{
    [Header("Watch this (same object as Power Button's 'Screen On')")]
    public GameObject screenOn;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip startupSound;   // "opening..."
    public AudioClip shutdownSound;  // "closing..." or "shutdown"

    bool wasOn;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        wasOn = screenOn != null && screenOn.activeSelf;   // no sound at scene start
    }

    void Update()
    {
        if (screenOn == null) return;

        bool isOn = screenOn.activeSelf;
        if (isOn == wasOn) return;

        Play(isOn ? startupSound : shutdownSound);
        wasOn = isOn;
    }

    void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.Stop();            // cut the other sound if still playing
        audioSource.PlayOneShot(clip);
    }
}