using System.Collections;
using UnityEngine;

// Attach this near the PSU. Configure the ParticleSystem in the Inspector
// (see setup notes) -- this script just triggers it and adds a light flicker.
public class ShortCircuitEffect : MonoBehaviour
{
    [Header("Effects")]
    public ParticleSystem sparks;
    public Light flickerLight;
    public AudioSource crackleSound;

    [Header("Flicker Settings")]
    public float flickerDuration = 0.6f;
    public float flickerInterval = 0.05f;

    public void PlayEffect()
    {
        if (sparks != null)
            sparks.Play();

        if (crackleSound != null)
            crackleSound.Play();

        if (flickerLight != null)
            StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        float timer = 0f;
        bool originalState = flickerLight.enabled;

        while (timer < flickerDuration)
        {
            flickerLight.enabled = !flickerLight.enabled;
            timer += flickerInterval;
            yield return new WaitForSeconds(flickerInterval);
        }

        flickerLight.enabled = originalState;
    }
}