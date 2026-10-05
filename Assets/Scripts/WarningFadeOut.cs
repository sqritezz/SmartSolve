using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class WarningFadeOut : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject homeUI;

    public float fadeDuration = 2f;

    private bool skipped = false;

    void Start()
    {
        if (homeUI != null)
            homeUI.SetActive(false);

        canvasGroup.alpha = 1f;

        // Hook up "click anywhere to skip" if an interactable is present
        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnSkipClicked);
    }

    private void OnSkipClicked(SelectEnterEventArgs args)
    {
        Skip();
    }

    public void Skip()
    {
        if (skipped) return;
        skipped = true;

        StartCoroutine(FadeOutThenShowHome());
    }

    IEnumerator FadeOutThenShowHome()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);

        if (homeUI != null)
            homeUI.SetActive(true);
    }
}