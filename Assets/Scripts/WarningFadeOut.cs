using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

// Warning screen: stays, fades out, then shows Home.
// Pressing the TRIGGER anywhere skips it (needs TriggerClicker on the controllers).
public class WarningFadeOut : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject homeUI;

    public float stayTime = 3f;
    public float fadeDuration = 2f;
    [Tooltip("Ignore clicks for this long, so a leftover press doesn't skip instantly")]
    public float ignoreClicksFor = 0.5f;

    private Coroutine fadeRoutine;
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

        if (homeUI != null)
            homeUI.SetActive(false);

        // Still works if a ClickOnly + XR Simple Interactable is on the panel
        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(args => Skip());

        fadeRoutine = StartCoroutine(FadeOutThenShowHome());
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

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);

        if (homeUI != null)
            homeUI.SetActive(true);
    }

    IEnumerator FadeOutThenShowHome()
    {
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(stayTime);

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        Skip();
    }
}