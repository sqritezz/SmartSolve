using System.Collections;
using UnityEngine;

// Put this on a button that opens an animated panel (e.g. settingsbtn).
// Button On Click -> OpenPanel.Open
//
// Turns the panel ON, waits one frame (so its Animator is ready), then plays "Open".
// Safety: if the panel is still invisible (scale 0) after the animation time,
// it is set to its normal size so it can always be seen.
public class OpenPanel : MonoBehaviour
{
    [Tooltip("The panel to open (e.g. Settingspanel)")]
    public GameObject panel;
    [Tooltip("Animator trigger that opens it")]
    public string openTrigger = "Open";
    [Tooltip("Size the panel should have when open")]
    public Vector3 openScale = Vector3.one;
    [Tooltip("Seconds to wait before the safety check")]
    public float safetyDelay = 1f;

    public void Open()
    {
        if (panel == null) return;
        StopAllCoroutines();
        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        panel.SetActive(true);
        yield return null;                       // let the Animator start up

        Animator anim = panel.GetComponent<Animator>();
        if (anim != null) anim.enabled = true;
        if (anim != null && anim.runtimeAnimatorController != null && !string.IsNullOrEmpty(openTrigger))
        {
            anim.ResetTrigger(openTrigger);
            anim.SetTrigger(openTrigger);
        }

        yield return new WaitForSecondsRealtime(safetyDelay);
        if (panel.activeInHierarchy && panel.transform.localScale.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("[OpenPanel] " + panel.name + " was still at scale 0 - the '" + openTrigger +
                             "' animation didn't play. Forcing it open.", panel);
            if (anim != null) anim.enabled = false;   // stop the Animator from shrinking it again
            panel.transform.localScale = openScale;
        }
    }
}