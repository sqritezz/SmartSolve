using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Options menu: COMFORT settings (saved, so they stay after closing the game).
//   Turning        : Snap / Smooth                  -> button On Click: ComfortOptions.ToggleTurning
//   Snap angle     : 30 / 45 / 90                   -> button On Click: ComfortOptions.NextSnapAngle
//   Movement speed : Slow / Normal / Fast          -> button On Click: ComfortOptions.NextMoveSpeed
//   Vignette       : On / Off                       -> button On Click: ComfortOptions.ToggleVignette
// Put ONE on the Option panel. Finds the XR Origin's turn/move providers by itself.
public class ComfortOptions : MonoBehaviour
{
    [Header("Found automatically if empty (drag the provider components here if not)")]
    [Tooltip("The Snap Turn Provider component")]
    public Behaviour snapTurn;
    [Tooltip("The Continuous Turn Provider component")]
    public Behaviour smoothTurn;
    [Tooltip("The Continuous / Dynamic Move Provider component (joystick walking)")]
    public Behaviour smoothMove;
    [Tooltip("Optional: the Tunneling Vignette object (child of Main Camera in the XR starter rig). " +
             "Leave empty if you don't have one - the vignette option then does nothing.")]
    public GameObject vignette;

    [Header("Texts that show the current choice (optional)")]
    public TMP_Text turningText;
    public TMP_Text snapAngleText;
    public TMP_Text movementText;
    public TMP_Text vignetteText;

    [Header("Saving")]
    [Tooltip("Unticked = every Play / every game start uses the Defaults below (choices only last until the game closes).\n" +
             "Ticked = the player's choices are remembered after closing the game.")]
    public bool rememberBetweenSessions = false;

    [Header("Defaults (used at every start while 'Remember' is unticked)")]
    public bool snapTurning = true;
    public float snapAngle = 45f;
    [Tooltip("0 = Slow, 1 = Normal, 2 = Fast")]
    public int moveSpeedLevel = 1;
    [Tooltip("Walking speed (meters per second) for Slow, Normal, Fast")]
    public float[] moveSpeeds = { 1f, 2f, 3f };
    public bool vignetteOn = false;

    private static readonly float[] Angles = { 30f, 45f, 90f };

    // ---- Shared between ALL copies (Home settings + Pause settings) ----
    private static readonly List<ComfortOptions> all = new List<ComfortOptions>();
    private static bool sInit;
    private static bool sSnap, sVig;
    private static float sAngle;
    private static int sSpeed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetShared() { all.Clear(); sInit = false; }

    private void WriteShared() { sSnap = snapTurning; sAngle = snapAngle; sSpeed = moveSpeedLevel; sVig = vignetteOn; }
    private void ReadShared() { snapTurning = sSnap; snapAngle = sAngle; moveSpeedLevel = sSpeed; vignetteOn = sVig; }

    private void OnDestroy() { all.Remove(this); }

    private void Awake()
    {
        FindProviders();
        if (!all.Contains(this)) all.Add(this);

        // Another copy already started: use the SAME current settings, don't reset them
        if (sInit) { ReadShared(); ApplyAll(); return; }
        sInit = true;

        if (!rememberBetweenSessions)
        {
            // Fresh start every time: forget old choices, use the Defaults
            PlayerPrefs.DeleteKey("SS_SnapTurn");
            PlayerPrefs.DeleteKey("SS_SnapAngle");
            PlayerPrefs.DeleteKey("SS_MoveSpeed");
            PlayerPrefs.DeleteKey("SS_Vignette");
            WriteShared();
            ApplyAll();
            return;
        }

        // Load saved choices
        snapTurning = PlayerPrefs.GetInt("SS_SnapTurn", snapTurning ? 1 : 0) == 1;
        snapAngle = PlayerPrefs.GetFloat("SS_SnapAngle", snapAngle);
        moveSpeedLevel = PlayerPrefs.GetInt("SS_MoveSpeed", moveSpeedLevel);
        vignetteOn = PlayerPrefs.GetInt("SS_Vignette", vignetteOn ? 1 : 0) == 1;
        WriteShared();
        ApplyAll();
    }

    private void OnEnable()
    {
        if (sInit) ReadShared();
        LanguageManager.OnLanguageChanged += RefreshTexts;
        RefreshTexts();
    }

    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -= RefreshTexts;
    }

    // ---------- Buttons ----------
    public void ToggleTurning() { snapTurning = !snapTurning; SaveApply(); }
    public void NextMoveSpeed() { moveSpeedLevel = (moveSpeedLevel + 1) % moveSpeeds.Length; SaveApply(); }
    // Old name, so a Movement button still wired to ToggleMovement keeps working
    public void ToggleMovement() { NextMoveSpeed(); }
    public void ToggleVignette() { vignetteOn = !vignetteOn; SaveApply(); }

    public void NextSnapAngle()
    {
        int i = System.Array.IndexOf(Angles, snapAngle);
        snapAngle = Angles[(i + 1) % Angles.Length];
        SaveApply();
    }

    // For UI Toggles (checkboxes), if you prefer those
    public void SetSnapTurning(bool on) { snapTurning = on; SaveApply(); }
    public void SetVignette(bool on) { vignetteOn = on; SaveApply(); }

    // ---------- Apply ----------
    private void SaveApply()
    {
        // Share the change with every copy (Home + Pause), so both show the same thing
        WriteShared();
        foreach (var c in all) if (c != null && c != this) { c.ReadShared(); c.RefreshTexts(); }

        if (!rememberBetweenSessions) { ApplyAll(); return; }
        PlayerPrefs.SetInt("SS_SnapTurn", snapTurning ? 1 : 0);
        PlayerPrefs.SetFloat("SS_SnapAngle", snapAngle);
        PlayerPrefs.SetInt("SS_MoveSpeed", moveSpeedLevel);
        PlayerPrefs.SetInt("SS_Vignette", vignetteOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAll();
    }

    private void ApplyAll()
    {
        if (snapTurn != null)
        {
            snapTurn.enabled = snapTurning;
            var prop = snapTurn.GetType().GetProperty("turnAmount");
            if (prop != null && prop.CanWrite) prop.SetValue(snapTurn, snapAngle);
        }
        if (smoothTurn != null) smoothTurn.enabled = !snapTurning;
        if (smoothMove != null)
        {
            smoothMove.enabled = true;   // joystick walking always on
            moveSpeedLevel = Mathf.Clamp(moveSpeedLevel, 0, moveSpeeds.Length - 1);
            var sp = smoothMove.GetType().GetProperty("moveSpeed");
            if (sp != null && sp.CanWrite) sp.SetValue(smoothMove, moveSpeeds[moveSpeedLevel]);
        }
        if (vignette != null) vignette.SetActive(vignetteOn);
        RefreshTexts();
    }

    // Only the CURRENT CHOICE goes on the buttons (e.g. "Snap", "45°", "ON").
    // The labels beside the buttons ("Turning:", "Pag-ikot:"...) use LocalizedText.
    private void RefreshTexts()
    {
        Set(turningText, snapTurning ? "Snap" : LanguageManager.Pick("Smooth", "Tuloy-tuloy"));
        Set(snapAngleText, snapAngle + "°");
        string[] en = { "Slow", "Normal", "Fast" };
        string[] fil = { "Mabagal", "Normal", "Mabilis" };
        int i = Mathf.Clamp(moveSpeedLevel, 0, 2);
        Set(movementText, LanguageManager.Pick(en[i], fil[i]));
        Set(vignetteText, vignetteOn ? LanguageManager.Pick("ON", "BUKAS") : LanguageManager.Pick("OFF", "SARADO"));
    }

    private static void Set(TMP_Text t, string s) { if (t != null) t.text = s; }

    private void FindProviders()
    {
        // Found by name, so it works with any XR Interaction Toolkit version
        foreach (var b in FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = b.GetType().Name;
            if (snapTurn == null && n.Contains("SnapTurnProvider")) snapTurn = b;
            else if (smoothTurn == null && n.Contains("ContinuousTurnProvider")) smoothTurn = b;
            else if (smoothMove == null && (n.Contains("ContinuousMoveProvider") || n.Contains("DynamicMoveProvider"))) smoothMove = b;
        }

        if (snapTurn == null) Debug.LogWarning("[ComfortOptions] No Snap Turn Provider found on the XR Origin - add one so Snap turning works.");
        if (smoothTurn == null) Debug.LogWarning("[ComfortOptions] No Continuous Turn Provider found on the XR Origin - add one so Smooth turning works.");
    }
}