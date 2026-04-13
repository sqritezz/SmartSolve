using System.Collections;
using UnityEngine;

public class PowerButton : MonoBehaviour
{
    public Renderer monitorRenderer;
    public Material screenOn;
    public Material screenOff;

    public float onDuration = 3f;

    public void TurnOnMonitor()
    {
        StartCoroutine(BootSequence());
    }

    IEnumerator BootSequence()
    {
        // Turn ON
        monitorRenderer.material = screenOn;

        // Wait
        yield return new WaitForSeconds(onDuration);

        // Turn OFF (blackout)
        monitorRenderer.material = screenOff;
    }
}