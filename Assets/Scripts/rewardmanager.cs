using System.Collections.Generic;
using UnityEngine;

// Put this on an empty GameObject that exists once in your first/main scene
// (e.g. "RewardManager"). It survives scene loads, so your assembly area
// can check UnlockedParts.Contains("Monitor") etc. from any scene.
public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    [Tooltip("Filled in automatically as parts are unlocked. View-only in Inspector.")]
    public List<string> unlockedParts = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Call this when a level is completed, e.g. RewardManager.Instance.UnlockPart("Monitor");
    public void UnlockPart(string partName)
    {
        if (!unlockedParts.Contains(partName))
        {
            unlockedParts.Add(partName);
            Debug.Log("Part unlocked: " + partName);
        }
    }

    public bool IsPartUnlocked(string partName)
    {
        return unlockedParts.Contains(partName);
    }
}