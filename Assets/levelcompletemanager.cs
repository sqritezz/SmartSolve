using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach this to a manager object for EACH stage/difficulty (e.g. one on
// ChecklistEasy's GameObject, or a dedicated "EasyLevelComplete" object).
// Hook the ChecklistManager's "On All Objectives Complete" UnityEvent (in the
// Inspector) to call this script's ShowRewardScreen() method.
public class LevelCompleteManager : MonoBehaviour
{
    [Header("Reward Panel")]
    [Tooltip("The panel that pops up when the level is finished (congrats + stars)")]
    public GameObject rewardPanel;

    [Tooltip("Star icons in order -- how many light up depends on starsEarned")]
    public GameObject[] starIcons;

    [Range(0, 5)]
    public int starsEarned = 3;

    [Header("Text")]
    public TextMeshProUGUI congratsText;
    public string congratsMessage = "Level Complete!";

    [Header("Reward")]
    [Tooltip("Name of the part this level unlocks, e.g. 'Monitor', 'Mouse', 'Keyboard'")]
    public string rewardPartName;
    public TextMeshProUGUI rewardText; // e.g. "You unlocked: Monitor"

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip congratsSound;

    private void Awake()
    {
        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        // Start with all stars hidden; ShowRewardScreen will reveal the earned ones
        SetStarsVisible(0);
    }

    // Call this from ChecklistManager's onAllObjectivesComplete UnityEvent
    public void ShowRewardScreen()
    {
        if (congratsText != null)
            congratsText.text = congratsMessage;

        if (rewardText != null && !string.IsNullOrEmpty(rewardPartName))
            rewardText.text = "You unlocked: " + rewardPartName;

        if (rewardPanel != null)
            rewardPanel.SetActive(true);

        if (audioSource != null && congratsSound != null)
            audioSource.PlayOneShot(congratsSound);

        SetStarsVisible(starsEarned);

        if (!string.IsNullOrEmpty(rewardPartName) && RewardManager.Instance != null)
            RewardManager.Instance.UnlockPart(rewardPartName);
    }

    private void SetStarsVisible(int count)
    {
        if (starIcons == null) return;

        for (int i = 0; i < starIcons.Length; i++)
        {
            if (starIcons[i] != null)
                starIcons[i].SetActive(i < count);
        }
    }
}