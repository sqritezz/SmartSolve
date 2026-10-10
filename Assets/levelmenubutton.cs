using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Press Y (left controller) to open the menu panel that matches
// the level the player is currently in. Press Y again to close it.
//
// It works out "which level am I in?" by finding the closest
// level spawn point to the player.
//   Near EasySpawnPoint (RAM)  -> opens RamTesting
//   Near a PSU spawn point     -> opens PSUTroubleshooting
//   ...and so on for whatever you add to the list.
public class LevelMenuButton : MonoBehaviour
{
    [System.Serializable]
    public class LevelPanel
    {
        [Tooltip("Just a note for yourself, e.g. 'RAM Easy'")]
        public string label;
        [Tooltip("That level's spawn point, e.g. EasySpawnPoint (RAM)")]
        public Transform spawnPoint;
        [Tooltip("The panel to show while in this level, e.g. RamTesting")]
        public GameObject panel;
    }

    [Header("Player")]
    [Tooltip("Drag XR Origin (VR) here")]
    public Transform player;

    [Header("Menu")]
    [Tooltip("The parent that holds all the panels (Mainnnn)")]
    public GameObject menuRoot;
    [Tooltip("Every panel inside Mainnnn (selectlearningpath, RamTesting, PSUTroubleshooting, HDD&Motherboard, ProceedToQuiz...). These get hidden before the right one is shown.")]
    public List<GameObject> allPanels = new List<GameObject>();

    [Header("Levels (press + to add one per level)")]
    public List<LevelPanel> levels = new List<LevelPanel>();
    [Tooltip("Only counts a spawn point if the player is within this many meters of it")]
    public float maxDistance = 30f;
    [Tooltip("Shown when the player isn't near any level (optional, e.g. selectlearningpath)")]
    public GameObject fallbackPanel;

    [Header("Audio (optional)")]
    public AudioSource openSound;
    public AudioSource closeSound;

    private bool wasPressed;
    private GameObject currentPanel;

    private void Update()
    {
        bool pressed = false;
        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.isValid)
            leftHand.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed); // Y button

        if (pressed && !wasPressed)
            Toggle();

        wasPressed = pressed;
    }

    public void Toggle()
    {
        if (currentPanel != null && currentPanel.activeInHierarchy)
            Close();
        else
            Open();
    }

    public void Open()
    {
        GameObject target = FindPanelForCurrentLevel();
        if (target == null) return;

        if (menuRoot != null) menuRoot.SetActive(true);
        foreach (var p in allPanels)
            if (p != null) p.SetActive(false);

        target.SetActive(true);
        currentPanel = target;

        if (openSound) openSound.Play();
    }

    public void Close()
    {
        if (currentPanel != null) currentPanel.SetActive(false);
        if (menuRoot != null) menuRoot.SetActive(false);
        currentPanel = null;

        if (closeSound) closeSound.Play();
    }

    private GameObject FindPanelForCurrentLevel()
    {
        if (player == null) return fallbackPanel;

        LevelPanel closest = null;
        float best = maxDistance;

        foreach (var lvl in levels)
        {
            if (lvl.spawnPoint == null || lvl.panel == null) continue;
            float d = Vector3.Distance(player.position, lvl.spawnPoint.position);
            if (d < best)
            {
                best = d;
                closest = lvl;
            }
        }

        return closest != null ? closest.panel : fallbackPanel;
    }
}