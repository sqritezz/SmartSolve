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

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("First press -- completes regardless of outcome (the 'see it fail' moment)")]
    public int observeGroupIndex = 0;
    public int observeObjectiveIndex = 0;
    [Tooltip("The 'click power again, try RAM on table' step -- also completes regardless of outcome")]
    public int retryGroupIndex = 2;
    public int retryObjectiveIndex = 0;
    [Tooltip("Final press -- only completes if workingRamInserted is true")]
    public int successGroupIndex = 2;
    public int successObjectiveIndex = 4;

    [Header("Scoring")]
    public PerformanceTracker performanceTracker;

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

        bool isObserveStep = checklist != null && checklist.IsCurrentStep(observeGroupIndex, observeObjectiveIndex);
        bool isRetryStep = checklist != null && checklist.IsCurrentStep(retryGroupIndex, retryObjectiveIndex);

        if (isObserveStep)
            checklist.CompleteObjective(observeGroupIndex, observeObjectiveIndex);

        if (isRetryStep)
            checklist.CompleteObjective(retryGroupIndex, retryObjectiveIndex);

        if (workingRamInserted)
        {
            ShowScreenOn();

            if (beepSound != null)
                beepSound.Stop();

            if (checklist != null && checklist.IsCurrentStep(successGroupIndex, successObjectiveIndex))
                checklist.CompleteObjective(successGroupIndex, successObjectiveIndex);
        }
        else
        {
            ShowScreenOff();

            if (beepSound != null && !beepSound.isPlaying)
                beepSound.Play();

            // Only count as a mistake if this wasn't one of the expected "try it and see" presses
            if (!isObserveStep && !isRetryStep && performanceTracker != null)
                performanceTracker.RegisterWrongAttempt();
        }
    }

    public void BrokenRamInserted()
    {
        workingRamInserted = false;
        Debug.Log("Broken RAM inserted");
    }

    public void BrokenRamRemoved()
    {
        workingRamInserted = false;
        Debug.Log("Broken RAM removed");
    }

    public void BrokenRamCleaned()
    {
        workingRamInserted = false;
        Debug.Log("Broken RAM cleaned, but still not fixed");
    }

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