using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HardPCManager : MonoBehaviour
{
    [Header("Monitor")]
    public GameObject screenOn;
    public GameObject screenOff;

    [Header("Audio")]
    public AudioSource beepSound;

    [Header("State")]
    public bool workingRamInserted = false;

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
        Debug.Log("Hard Power Pressed | workingRamInserted = " + workingRamInserted);

        if (workingRamInserted)
        {
            ShowScreenOn();

            if (beepSound != null)
                beepSound.Stop();
        }
        else
        {
            ShowScreenOff();

            if (beepSound != null && !beepSound.isPlaying)
                beepSound.Play();
        }
    }

    // Broken RAM inserted into slot
    public void BrokenRamInserted()
    {
        workingRamInserted = false;
        Debug.Log("Broken RAM inserted");
    }

    // Broken RAM removed from slot
    public void BrokenRamRemoved()
    {
        workingRamInserted = false;
        Debug.Log("Broken RAM removed");
    }

    // Broken RAM cleaned with eraser
    public void BrokenRamCleaned()
    {
        workingRamInserted = false;
        Debug.Log("Broken RAM cleaned, but still not fixed");
    }

    // New Working RAM inserted
    public void WorkingRamInserted()
    {
        workingRamInserted = true;

        if (beepSound != null)
            beepSound.Stop();

        Debug.Log("Working RAM inserted");
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