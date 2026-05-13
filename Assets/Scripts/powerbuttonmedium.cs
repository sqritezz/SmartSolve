using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PowerButtonMedium : MonoBehaviour
{
    public GameObject screenOn;
    public GameObject screenOff;

    public AudioSource beepSound;

    public float turnOffDelay = 3f;

    public bool ramFixed = false;

    private XRBaseInteractable interactable;
    private Coroutine bootRoutine;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        interactable.selectEntered.AddListener(OnPowerPressed);
    }

    void Start()
    {
        screenOn.SetActive(false);
        screenOff.SetActive(true);
    }

    void OnPowerPressed(SelectEnterEventArgs args)
    {
        if (bootRoutine != null)
            StopCoroutine(bootRoutine);

        bootRoutine = StartCoroutine(BootMonitor());
    }

    IEnumerator BootMonitor()
    {
        screenOn.SetActive(true);
        screenOff.SetActive(false);

        if (!ramFixed)
        {
            if (beepSound != null)
                beepSound.Play();

            yield return new WaitForSeconds(turnOffDelay);

            screenOn.SetActive(false);
            screenOff.SetActive(true);
        }
        else
        {
            if (beepSound != null)
                beepSound.Stop();

            screenOn.SetActive(true);
            screenOff.SetActive(false);
        }
    }

    public void FixRAM()
    {
        ramFixed = true;

        if (beepSound != null)
            beepSound.Stop();

        screenOn.SetActive(true);
        screenOff.SetActive(false);
    }
}