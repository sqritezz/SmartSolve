using UnityEngine;
using System.Collections;

public class WarningFadeOut : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject homeUI;

    public float stayTime = 3f;
    public float fadeDuration = 2f;

    void Start()
    {
        if (homeUI != null)
            homeUI.SetActive(false);

        StartCoroutine(FadeOutThenShowHome());
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

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);

        if (homeUI != null)
            homeUI.SetActive(true);
    }
}