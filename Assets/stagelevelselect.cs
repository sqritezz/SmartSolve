using UnityEngine;

// Pause menu "Select Level" button.
// Opens the level-select panel of the STAGE the player is in now:
//   in RAM Easy / Medium / Hard -> RAM panel,  PSU ... -> PSU panel,  GPU ... -> GPU panel.
//
// Select Level button  On Click -> StageLevelSelect.Open
// A Back / Close button On Click -> StageLevelSelect.Close
public class StageLevelSelect : MonoBehaviour
{
    [Header("Level-select panel for each stage")]
    public GameObject ramPanel;
    public GameObject psuPanel;
    public GameObject gpuPanel;
    [Tooltip("Optional: shown in the Final stage")]
    public GameObject finalPanel;
    [Tooltip("Shown when the player isn't in any stage (menu room, PC room)")]
    public GameObject fallbackPanel;

    [Header("Optional")]
    [Tooltip("Dark overlay behind the panel (e.g. Panel3). Turned on with the panel, off on Close.")]
    public GameObject overlay;

    private GameObject openNow;

    public void Open()
    {
        CloseAll();

        string level = TeleportToPoint.CurrentLevel ?? "";
        GameObject p = fallbackPanel;
        if (level.StartsWith("RAM")) p = ramPanel;
        else if (level.StartsWith("PSU")) p = psuPanel;
        else if (level.StartsWith("GPU")) p = gpuPanel;
        else if (level.StartsWith("Final")) p = finalPanel != null ? finalPanel : fallbackPanel;

        if (p == null)
        {
            Debug.LogWarning("[StageLevelSelect] No panel set for level '" + level + "'.", this);
            return;
        }
        if (overlay != null) overlay.SetActive(true);
        p.SetActive(true);
        openNow = p;
    }

    public void Close()
    {
        CloseAll();
    }

    private void CloseAll()
    {
        foreach (var g in new[] { ramPanel, psuPanel, gpuPanel, finalPanel, fallbackPanel })
            if (g != null) g.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        openNow = null;
    }
}