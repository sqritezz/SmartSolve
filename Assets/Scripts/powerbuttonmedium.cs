using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PowerButtonMedium : MonoBehaviour
{
    public GameObject screenOn;
    public GameObject screenOff;
    public AudioSource beepSound;

    public bool ramFixed = false;

    private XRBaseInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        if (interactable != null)
            interactable.selectEntered.AddListener(OnPowerPressed);
    }

    void Start()
    {
        ShowScreenOff();

        if (beepSound != null)
            beepSound.Stop();
    }

    void OnPowerPressed(SelectEnterEventArgs args)
    {
        PressPowerButton();
    }

    public void PressPowerButton()
    {
        Debug.Log("Power pressed. ramFixed = " + ramFixed);

        if (ramFixed)
        {
            ShowScreenOn();

            if (beepSound != null)
                beepSound.Stop();
        }
        else
        {
            ShowScreenOff();

            if (beepSound != null)
                beepSound.Play();
        }
    }

    void ShowScreenOn()
    {
        screenOn.SetActive(true);
        screenOff.SetActive(false);
    }

    void ShowScreenOff()
    {
        screenOn.SetActive(false);
        screenOff.SetActive(true);
    }
}