using System.Collections;
using UnityEngine;

public class PowerButton : MonoBehaviour
{
    [Header("Monitor Settings")]
    public Renderer monitorRenderer;
    public Material screenOn;
    public Material screenOff;

    [Header("Boot Settings")]
    public float onDuration = 3f;

    [Header("System State")]
    public bool isRamInstalled = false; // becomes TRUE after reinserting RAM

    // Called when button is pressed
    public void TurnOnMonitor()
    {
        Debug.Log("BUTTON CLICKED");
        StartCoroutine(BootSequence());
    }

    IEnumerator BootSequence()
    {
        if (monitorRenderer == null)
        {
            Debug.LogWarning("Monitor Renderer is NOT assigned!");
            yield break;
        }

        // Turn ON
        monitorRenderer.material = screenOn;

        // Wait
        yield return new WaitForSeconds(onDuration);

        // Turn OFF only if RAM is NOT fixed
        if (!isRamInstalled)
        {
            monitorRenderer.material = screenOff;
            Debug.Log("System failed (RAM issue)");
        }
        else
        {
            Debug.Log("System running нормально 😎");
        }
    }
}