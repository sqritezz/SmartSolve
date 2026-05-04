using System.Collections;
using UnityEngine;

public class PowerButton : MonoBehaviour
{
    [Header("Monitor")]
    public GameObject monitorScreen;
    public Material screenOn;
    public Material screenOff;

    [Header("Boot Settings")]
    public float onDuration = 3f;

    [Header("System State")]
    public bool isRamInstalled = false;

    private bool firstBootDone = false;
    private Renderer monitorRenderer;

    void Start()
    {
        monitorRenderer = monitorScreen.GetComponent<Renderer>();
        TurnScreenOff();
    }

    public void PressPowerButton()
    {
        if (!firstBootDone)
        {
            StartCoroutine(FirstBootSequence());
        }
        else
        {
            if (isRamInstalled)
            {
                TurnScreenOn();
            }
            else
            {
                StartCoroutine(FailedBoot());
            }
        }
    }

    IEnumerator FirstBootSequence()
    {
        firstBootDone = true;

        TurnScreenOn();

        yield return new WaitForSeconds(onDuration);

        // Simulate faulty RAM shutdown
        TurnScreenOff();
    }

    IEnumerator FailedBoot()
    {
        TurnScreenOn();

        yield return new WaitForSeconds(2f);

        TurnScreenOff();
    }

    public void TurnScreenOn()
    {
        monitorRenderer.material = screenOn;
    }

    public void TurnScreenOff()
    {
        monitorRenderer.material = screenOff;
    }
}