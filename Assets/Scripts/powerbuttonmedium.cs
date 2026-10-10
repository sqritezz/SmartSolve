using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Power button for RAM Medium.
//   RAM NOT fixed -> beeps (the fault symptom), screen stays off.
//   RAM fixed     -> NO beep. Press once = PC turns on (Windows opening sound
//                    via Power Button Sounds). Press again = PC turns off.
public class PowerButtonMedium : MonoBehaviour
{
    public GameObject screenOn;
    public GameObject screenOff;
    [Tooltip("Not used anymore, kept so your Inspector value isn't lost")]
    public float turnOffDelay = 3f;
    public bool isRamFixed = false;
    public AudioSource beepSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int successGroupIndex = 3;
    public int successObjectiveIndex = 1;

    [Tooltip("Optional: group/objective that completes on ANY press (observe step). Set observeGroupIndex to -1 to disable.")]
    public int observeGroupIndex = -1;
    public int observeObjectiveIndex = 0;

    [Header("Scoring Integration")]
    public PerformanceTracker performanceTracker;

    private XRBaseInteractable interactable;
    private bool isPoweredOn = false;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnPowerPressed);
    }

    void Start()
    {
        ShowOff();
    }

    void OnPowerPressed(SelectEnterEventArgs args)
    {
        PressPowerButton();
    }

    public void PressPowerButton()
    {
        // Observe step: the first "test the PC" press
        bool isObserveStep = checklist != null && observeGroupIndex >= 0 &&
            checklist.IsCurrentStep(observeGroupIndex, observeObjectiveIndex);
        if (isObserveStep)
            checklist.CompleteObjective(observeGroupIndex, observeObjectiveIndex);

        if (!isRamFixed)
        {
            // Faulty RAM: beep once, screen stays off
            if (beepSound != null)
            {
                beepSound.Stop();
                beepSound.Play();
            }

            if (!isObserveStep && performanceTracker != null)
                performanceTracker.RegisterWrongAttempt();

            ShowOff();
            return;
        }

        // RAM fixed: no beep, normal on/off
        if (beepSound != null) beepSound.Stop();

        if (!isPoweredOn)
        {
            ShowOn();

            if (checklist != null && checklist.IsCurrentStep(successGroupIndex, successObjectiveIndex))
                checklist.CompleteObjective(successGroupIndex, successObjectiveIndex);
        }
        else
        {
            ShowOff();
        }
    }

    void ShowOn()
    {
        isPoweredOn = true;
        if (screenOn != null) screenOn.SetActive(true);
        if (screenOff != null) screenOff.SetActive(false);
    }

    void ShowOff()
    {
        isPoweredOn = false;
        if (screenOn != null) screenOn.SetActive(false);
        if (screenOff != null) screenOff.SetActive(true);
    }
}