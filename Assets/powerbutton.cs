using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PowerButton : MonoBehaviour
{
    public GameObject screenOn;
    public GameObject screenOff;

    public float turnOffDelay = 3f;
    public bool isRamFixed = false;

    private XRBaseInteractable interactable;
    private Coroutine bootRoutine;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        if (interactable != null)
            interactable.selectEntered.AddListener(OnPowerPressed);
    }

    void Start()
    {
        screenOn.SetActive(false);
        screenOff.SetActive(true);
    }

    void OnPowerPressed(SelectEnterEventArgs args)
    {
        PressPowerButton();
    }

    public void PressPowerButton()
    {
        if (bootRoutine != null)
            StopCoroutine(bootRoutine);

        bootRoutine = StartCoroutine(BootMonitor());
    }

    IEnumerator BootMonitor()
    {
        screenOn.SetActive(true);
        screenOff.SetActive(false);

        if (!isRamFixed)
        {
            yield return new WaitForSeconds(turnOffDelay);

            screenOn.SetActive(false);
            screenOff.SetActive(true);
        }
    }
}