using UnityEngine;

public class beepsound : MonoBehaviour
{
    public AudioSource beepSound;

    public void PlayMediumSound()
    {
        beepSound.Play();
    }
}