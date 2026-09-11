using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PowerButtonMedium : MonoBehaviour
{
    public GameObject screenOn;
    public GameObject screenOff;
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
        if (beepSound != null)
            beepSound.Play();

        if (bootRoutine != null)
            StopCoroutine(bootRoutine);
        bootRoutine = StartCoroutine(BootMonitor());
    }

    IEnumerator BootMonitor()
    {
        screenOn.SetActive(true);
        screenOff.SetActive(false);

        bool isObserveStep = checklist != null && observeGroupIndex >= 0 &&
            checklist.IsCurrentStep(observeGroupIndex, observeObjectiveIndex);

        if (isObserveStep)
        {
            checklist.CompleteObjective(observeGroupIndex, observeObjectiveIndex);
        }

        if (!isRamFixed)
        {
            if (!isObserveStep && performanceTracker != null)
                performanceTracker.RegisterWrongAttempt();

            yield return new WaitForSeconds(turnOffDelay);
            screenOn.SetActive(false);
            screenOff.SetActive(true);
        }
        else
        {
            if (checklist != null && checklist.IsCurrentStep(successGroupIndex, successObjectiveIndex))
            {
                checklist.CompleteObjective(successGroupIndex, successObjectiveIndex);
            }
        }
    }
}