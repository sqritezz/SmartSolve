using UnityEngine;
using UnityEngine.UI;

public class CreditsAutoScroll : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float speed = 0.05f; // lower = slower

    void OnEnable()
    {
        scrollRect.verticalNormalizedPosition = 1f; // start at top
    }

    void Update()
    {
        if (scrollRect.verticalNormalizedPosition > 0f)
            scrollRect.verticalNormalizedPosition -= speed * Time.unscaledDeltaTime;
    }
}