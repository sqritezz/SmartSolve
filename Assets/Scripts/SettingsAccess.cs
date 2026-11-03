using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ONE shared Settings panel used by both the Home menu and the Pause menu.
/// - From Home: the panel opens in its normal place (inside Home(UI)).
/// - From Pause: the panel is MOVED onto the pause canvas (Ypanelbtn) so it appears
///   exactly where the pause menu is, then moved back to Home(UI) when closed.
/// Put this on an object that is ALWAYS active (e.g. an empty "SettingsAccess" at the scene root).
/// </summary>
public class SettingsAccess : MonoBehaviour
{
    public enum Source { None, Home, Pause }

    [Header("Shared Settings Panel")]
    [Tooltip("Home(UI)/GameObject/Settingspanel")]
    public GameObject settingsPanel;

    [Tooltip("Optional: sub-panel to show first (e.g. generalpanel)")]
    public GameObject defaultSubPanel;
    [Tooltip("Optional: generalpanel, audiopanel, controllerpanel, CreditsPanel")]
    public GameObject[] subPanels;

    [Header("Pause Menu")]
    [Tooltip("Ypanelbtn - the pause CANVAS (the object with the Canvas component)")]
    public RectTransform pauseCanvas;

    [Tooltip("Size of the settings panel while it sits on the pause canvas. Make smaller/bigger if needed.")]
    public float scaleOnPause = 0.7f;
    [Tooltip("Move the panel a bit on the pause canvas if needed (canvas units)")]
    public Vector2 offsetOnPause = Vector2.zero;

    [Tooltip("Bring the panel closer to the player (metres). Try 0.2 - 0.5. Negative = push farther.")]
    public float pullCloserMeters = 0.3f;

    [Tooltip("While ON, Scale / Offset / Pull Closer update instantly in Play mode so you can tune them.")]
    public bool liveTuning = true;

    [Tooltip("Turn OFF the Animator on Settingspanel while it is on the pause canvas. " +
             "Its animation can overwrite position/scale and push the panel off-screen.")]
    public bool disableAnimatorOnPause = true;

    [Tooltip("Print debug info to the Console when opening from pause")]
    public bool debugLogs = true;

    Animator panelAnimator;
    bool animatorWasEnabled;
    int debugFramesLeft = -1;

    public Source CurrentSource { get; private set; } = Source.None;
    public bool IsOpen => settingsPanel != null && settingsPanel.activeInHierarchy;

    // ---- remembered original placement (inside Home(UI)) ----
    Transform originalParent;
    int originalSiblingIndex;
    Vector3 originalLocalPos;
    Quaternion originalLocalRot;
    Vector3 originalLocalScale;
    Vector2 originalAnchorMin, originalAnchorMax, originalPivot, originalAnchoredPos;

    // pause content we hid
    readonly List<GameObject> hiddenPauseChildren = new List<GameObject>();

    bool homePlacementSaved;

    void Awake()
    {
        if (settingsPanel == null) return;

        SaveHomePlacement();

        // Animator must keep working while the game is paused (Time.timeScale = 0)
        foreach (var anim in settingsPanel.GetComponentsInChildren<Animator>(true))
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    // ================= BUTTONS =================

    /// <summary>settingsbtn (Home menu) OnClick</summary>
    public void OpenFromHome()
    {
        if (settingsPanel == null) return;

        // If the panel was left on the pause canvas (e.g. went to Main menu while settings was open),
        // bring it back to Home(UI) first.
        if (CurrentSource == Source.Pause) CloseSettings();
        if (settingsPanel.transform.parent != originalParent)
        {
            Debug.LogWarning("[SettingsAccess] Panel was not in Home(UI) - moving it back");
            ReturnPanelToHome();
        }
        if (panelAnimator != null && !panelAnimator.enabled) panelAnimator.enabled = true;

        CurrentSource = Source.Home;
        Debug.Log("[SettingsAccess] Opening settings from HOME");

        // make sure every parent (Home(UI)/GameObject) is ON
        for (Transform p = settingsPanel.transform.parent; p != null; p = p.parent)
            if (!p.gameObject.activeSelf)
            {
                Debug.LogWarning($"[SettingsAccess] Parent was OFF, turning ON: {GetPath(p)}");
                p.gameObject.SetActive(true);
            }

        ShowSettings();

        // restart the Animator from the beginning so the open animation plays again
        var anim = settingsPanel.GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
            anim.Rebind();
            anim.Update(0f);
        }

        if (debugLogs)
        {
            Debug.Log("[SettingsAccess][DEBUG] --- HOME open ---");
            LogDebugInfo();
            debugFramesLeft = 2;
        }
    }

    /// <summary>optionbtn (Pause menu) OnClick</summary>
    public void OpenFromPause()
    {
        if (settingsPanel == null || pauseCanvas == null)
        {
            Debug.LogWarning("[SettingsAccess] Assign settingsPanel and pauseCanvas in the Inspector!");
            return;
        }

        CurrentSource = Source.Pause;
        Debug.Log("[SettingsAccess] Opening settings from PAUSE");

        // 1. make sure we know where the panel lives in Home(UI)
        //    (saved once in Awake - never re-saved here, so it can't get lost)
        var rt = (RectTransform)settingsPanel.transform;
        if (!homePlacementSaved) SaveHomePlacement();

        // 2. hide the pause menu's own content (bg, title, buttons) but keep the canvas ON
        hiddenPauseChildren.Clear();
        foreach (Transform child in pauseCanvas)
        {
            if (child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(false);
                hiddenPauseChildren.Add(child.gameObject);
            }
        }
        pauseCanvas.gameObject.SetActive(true);

        // 3. move the settings panel onto the pause canvas, centered
        rt.SetParent(pauseCanvas, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localRotation = Quaternion.identity;
        ApplyPausePlacement();
        rt.SetAsLastSibling();

        // stop the Animator from moving/scaling the panel away
        panelAnimator = settingsPanel.GetComponent<Animator>();
        if (panelAnimator != null)
        {
            animatorWasEnabled = panelAnimator.enabled;
            if (disableAnimatorOnPause) panelAnimator.enabled = false;
        }

        ShowSettings();

        if (debugLogs)
        {
            Debug.Log("[SettingsAccess][DEBUG] --- right after opening ---");
            LogDebugInfo();
            debugFramesLeft = 2; // log again 2 frames later (after Animator/other scripts run)
        }
    }

    // Size + position of the panel on the pause canvas
    void ApplyPausePlacement()
    {
        var rt = (RectTransform)settingsPanel.transform;
        rt.anchoredPosition = offsetOnPause;

        // convert metres -> canvas units (canvas scale is ~0.001, so 0.3 m = -300 units)
        float canvasScaleZ = Mathf.Abs(pauseCanvas.lossyScale.z);
        float z = canvasScaleZ > 0f ? -pullCloserMeters / canvasScaleZ : 0f;
        rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, z);

        rt.localScale = Vector3.one * scaleOnPause;
    }

    void LateUpdate()
    {
        // live tuning: change Scale / Offset / Pull Closer in Play mode and see it instantly
        if (liveTuning && CurrentSource == Source.Pause && IsOpen)
            ApplyPausePlacement();

        if (debugFramesLeft < 0) return;
        if (debugFramesLeft-- == 0)
        {
            Debug.Log("[SettingsAccess][DEBUG] --- 2 frames later ---");
            LogDebugInfo();
        }
    }

    // Logs WHY the panel might be invisible
    void LogDebugInfo()
    {
        var t = settingsPanel.transform;
        var rtp = (RectTransform)t;
        Debug.Log($"[SettingsAccess][DEBUG] localScale = {t.localScale}, anchoredPos = {rtp.anchoredPosition}, size = {rtp.rect.size}, " +
                  $"Animator = {(panelAnimator ? (panelAnimator.enabled ? "ON" : "OFF") : "none")}");

        Debug.Log($"[SettingsAccess][DEBUG] Settingspanel activeInHierarchy = {settingsPanel.activeInHierarchy}, " +
                  $"parent = {(t.parent ? t.parent.name : "none")}, Ypanelbtn active = {(pauseCanvas ? pauseCanvas.gameObject.activeInHierarchy.ToString() : "not set")}");

        // first parent that is turned OFF
        for (Transform p = t; p != null; p = p.parent)
            if (!p.gameObject.activeSelf)
                Debug.LogWarning($"[SettingsAccess][DEBUG] INACTIVE parent: {GetPath(p)}");

        // disabled Canvas / invisible CanvasGroup above the panel
        foreach (var c in settingsPanel.GetComponentsInParent<Canvas>(true))
            if (!c.enabled) Debug.LogWarning($"[SettingsAccess][DEBUG] Canvas component DISABLED on {GetPath(c.transform)}");
        foreach (var g in settingsPanel.GetComponentsInParent<CanvasGroup>(true))
            if (g.alpha < 0.99f) Debug.LogWarning($"[SettingsAccess][DEBUG] CanvasGroup alpha = {g.alpha} on {GetPath(g.transform)}");

        Debug.Log($"[SettingsAccess][DEBUG] Settingspanel world scale = {t.lossyScale}, world pos = {t.position}");

        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 v = cam.WorldToViewportPoint(t.position);
            Debug.Log($"[SettingsAccess][DEBUG] Distance from camera = {Vector3.Distance(cam.transform.position, t.position):F2} m, " +
                      $"in front of camera = {v.z > 0}, on screen = {v.x > 0 && v.x < 1 && v.y > 0 && v.y < 1}");
        }
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }

    /// <summary>X button and Backbtn on Settingspanel OnClick</summary>
    public void CloseSettings()
    {
        if (settingsPanel == null) return;
        Debug.Log($"[SettingsAccess] CloseSettings (source = {CurrentSource})");
        settingsPanel.SetActive(false);
        debugFramesLeft = -1;

        if (CurrentSource == Source.Pause)
        {
            if (panelAnimator != null) panelAnimator.enabled = animatorWasEnabled;

            Debug.Log("[SettingsAccess] Closing settings -> back to PAUSE");
            ReturnPanelToHome();

            // show the pause menu content again
            foreach (var go in hiddenPauseChildren)
                if (go != null) go.SetActive(true);
            hiddenPauseChildren.Clear();
        }
        else
        {
            Debug.Log("[SettingsAccess] Closing settings -> back to HOME");
        }

        CurrentSource = Source.None;
    }

    /// <summary>
    /// Call this from your Resume / Restart / Main menu code if settings might still be open,
    /// so the panel always goes back to Home(UI).
    /// </summary>
    public void ForceClose()
    {
        if (CurrentSource != Source.None) CloseSettings();
    }

    // ================= HELPERS =================

    void ShowSettings()
    {
        settingsPanel.SetActive(true);

        if (defaultSubPanel != null)
        {
            foreach (var p in subPanels) if (p != null) p.SetActive(false);
            defaultSubPanel.SetActive(true);
        }
    }

    void SaveHomePlacement()
    {
        var rt = (RectTransform)settingsPanel.transform;
        originalParent = rt.parent;
        originalSiblingIndex = rt.GetSiblingIndex();
        originalLocalPos = rt.localPosition;
        originalLocalRot = rt.localRotation;
        originalLocalScale = rt.localScale;
        originalAnchorMin = rt.anchorMin;
        originalAnchorMax = rt.anchorMax;
        originalPivot = rt.pivot;
        originalAnchoredPos = rt.anchoredPosition;
        homePlacementSaved = true;
    }

    void ReturnPanelToHome()
    {
        if (originalParent == null) return;

        var rt = (RectTransform)settingsPanel.transform;
        rt.SetParent(originalParent, false);
        rt.SetSiblingIndex(originalSiblingIndex);
        rt.anchorMin = originalAnchorMin;
        rt.anchorMax = originalAnchorMax;
        rt.pivot = originalPivot;
        rt.anchoredPosition = originalAnchoredPos;
        rt.localPosition = originalLocalPos;
        rt.localRotation = originalLocalRot;
        rt.localScale = originalLocalScale;
    }
}