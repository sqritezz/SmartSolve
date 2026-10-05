using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach this to the PSU's power button (or reuse the PC's power button,
// whichever actually controls the monitor/PC state in your scene).
public class PSUPowerButton : MonoBehaviour
{
    [Header("Visual Feedback")]
    public GameObject screenOn;
    public GameObject screenOff;

    [Tooltip("How long it stays on before randomly shutting off, if the PSU isn't fixed yet")]
    public float randomShutdownDelay = 3f;

    [Header("State")]
    public bool isPsuFixed = false;

    [Header("Audio")]
    public AudioSource beepSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("First press -- completes regardless of outcome (the 'see it shut down' moment)")]
    public int observeGroupIndex = 0;
    public int observeObjectiveIndex = 0;
    [Tooltip("Final press -- only completes if isPsuFixed is true")]
    public int successGroupIndex = 2;
    public int successObjectiveIndex = 0;

    [Header("Scoring")]
    public PerformanceTracker performanceTracker;

    private XRBaseInteractable interactable;
    private Coroutine shutdownRoutine;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnPressed);
    }

    void Start()
    {
        ShowOff();
    }

    void OnPressed(SelectEnterEventArgs args)
    {
        PressPowerButton();
    }

    public void PressPowerButton()
    {
        if (beepSound != null)
            beepSound.Play();

        if (shutdownRoutine != null)
            StopCoroutine(shutdownRoutine);
        shutdownRoutine = StartCoroutine(BootSequence());
    }

    IEnumerator BootSequence()
    {
        ShowOn();

        bool isObserveStep = checklist != null && checklist.IsCurrentStep(observeGroupIndex, observeObjectiveIndex);

        if (isObserveStep)
            checklist.CompleteObjective(observeGroupIndex, observeObjectiveIndex);

        if (!isPsuFixed)
        {
            // Random shutdown/restart -- stays on briefly then cuts out
            yield return new WaitForSeconds(randomShutdownDelay);
            ShowOff();

            if (!isObserveStep && performanceTracker != null)
                performanceTracker.RegisterWrongAttempt();
        }
        else
        {
            if (checklist != null && checklist.IsCurrentStep(successGroupIndex, successObjectiveIndex))
                checklist.CompleteObjective(successGroupIndex, successObjectiveIndex);
        }
    }

    void ShowOn()
    {
        if (screenOn != null) screenOn.SetActive(true);
        if (screenOff != null) screenOff.SetActive(false);
    }

    void ShowOff()
    {
        if (screenOn != null) screenOn.SetActive(false);
        if (screenOff != null) screenOff.SetActive(true);
    }
}