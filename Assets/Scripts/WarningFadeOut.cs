using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

// Warning screen: stays on screen until the player presses the TRIGGER
// ("click anywhere to skip"), then quickly fades out and shows Home.
// Needs TriggerClicker on the controllers' Ray Interactors.
// (Optional) Tick "Auto Fade" if you also want it to leave on its own after a while.
public class WarningFadeOut : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject homeUI;

    [Header("Skipping")]
    [Tooltip("Ignore clicks for this long, so a leftover press doesn't skip instantly")]
    public float ignoreClicksFor = 0.5f;
    [Tooltip("How long the quick fade takes after clicking (0 = disappear instantly)")]
    public float skipFadeDuration = 0.3f;

    [Header("Auto Fade (off = wait for click forever)")]
    public bool autoFade = false;
    public float stayTime = 3f;
    public float fadeDuration = 2f;

    private Coroutine routine;
    private bool skipped = false;
    private float startTime;

    private void OnEnable()
    {
        TriggerClicker.OnAnyTriggerPressed += OnAnyClick;
    }

    private void OnDisable()
    {
        TriggerClicker.OnAnyTriggerPressed -= OnAnyClick;
    }

    void Start()
    {
        startTime = Time.time;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (homeUI != null)
            homeUI.SetActive(false);

        // Still works if a ClickOnly + XR Simple Interactable is on the panel
        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(args => OnAnyClick());

        if (autoFade)
            routine = StartCoroutine(AutoFadeRoutine());
    }

    private void OnAnyClick()
    {
        if (Time.time - startTime < ignoreClicksFor) return;
        Skip();
    }

    public void Skip()
    {
        if (skipped) return;
        skipped = true;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(FadeAndFinish(skipFadeDuration));
    }

    IEnumerator AutoFadeRoutine()
    {
        yield return new WaitForSeconds(stayTime);
        skipped = true;
        yield return FadeAndFinish(fadeDuration);
    }

    IEnumerator FadeAndFinish(float duration)
    {
        if (canvasGroup != null && duration > 0f)
        {
            float from = canvasGroup.alpha;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, 0f, timer / duration);
                yield return null;
            }
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (homeUI != null)
            homeUI.SetActive(true);

        gameObject.SetActive(false);
    }
}