using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

// Requires an XRSimpleInteractable on this same GameObject (or a child
// covering the panel) so "click anywhere" on the warning works via the
// player's ray + trigger. Make sure this object also has a Collider
// (e.g. a Box Collider sized to cover the full panel).
public class WarningFadeOut : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject homeUI;

    public float stayTime = 3f;
    public float fadeDuration = 2f;

    private Coroutine fadeRoutine;
    private bool skipped = false;

    void Start()
    {
        if (homeUI != null)
            homeUI.SetActive(false);

        // Hook up "click anywhere to skip" if an interactable is present
        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnSkipClicked);

        fadeRoutine = StartCoroutine(FadeOutThenShowHome());
    }

    private void OnSkipClicked(SelectEnterEventArgs args)
    {
        Skip();
    }

    // Call this from a UI Button's OnClick() too, if you'd rather use a
    // full-screen button instead of an XRSimpleInteractable.
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