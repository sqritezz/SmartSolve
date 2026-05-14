using UnityEngine;
using System.Collections;

public class WarningFadeOut : MonoBehaviour
{
    public CanvasGroup warningCanvas;
    public float stayTime = 3f;
    public float fadeDuration = 2f;

    void Start()
    {
        if (warningCanvas == null)
            warningCanvas = GetComponent<CanvasGroup>();

        StartCoroutine(FadeWarning());
    }

    IEnumerator FadeWarning()
    {
        warningCanvas.alpha = 1f;
        warningCanvas.interactable = false;
        warningCanvas.blocksRaycasts = false;

        yield return new WaitForSeconds(stayTime);

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            warningCanvas.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        warningCanvas.alpha = 0f;
        gameObject.SetActive(false);
    }
}