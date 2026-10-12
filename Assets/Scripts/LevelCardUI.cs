using UnityEngine;
using UnityEngine.UI;

// Put this on each level-select card (easybtn, mediumbtn, hardbtn).
// Reads LevelProgress to decide:
//  - whether this card is locked (the required previous difficulty isn't done yet)
//  - whether to show the "?" placeholder or the real completed artwork
//  - how many of the 3 stars to light up
public class LevelCardUI : MonoBehaviour
{
    [Header("Which level this card is")]
    [Tooltip("Groups Easy/Medium/Hard together, e.g. \"RAM\", \"PSU\", \"HDD\"")]
    public string category = "RAM";
    [Tooltip("\"Easy\", \"Medium\", or \"Hard\"")]
    public string difficulty = "Easy";

    [Header("Unlock requirement")]
    [Tooltip("Leave EMPTY for Easy (always unlocked). Medium card -> \"Easy\". Hard card -> \"Medium\".")]
    public string requiredPreviousDifficulty = "";

    [Header("Artwork")]
    [Tooltip("The Image component showing the '?' or the thumbnail (your 'Image' child object)")]
    public Image cardImage;
    [Tooltip("The '?' sprite shown before this level has been completed")]
    public Sprite placeholderSprite;
    [Tooltip("The real thumbnail shown once this level is completed")]
    public Sprite completedSprite;

    [Header("Stars (assign in order: 1st, 2nd, 3rd)")]
    public GameObject[] stars = new GameObject[3];

    [Header("Lock behavior")]
    [Tooltip("The Button component on this card")]
    public Button button;
    [Tooltip("Optional: a greyed-out overlay / lock icon shown while locked")]
    public GameObject lockedOverlay;

    private void OnEnable()
    {
        Refresh();
    }

    // Safe to call any time (e.g. when this screen opens) to re-check progress.
    public void Refresh()
    {
        bool unlocked = string.IsNullOrEmpty(requiredPreviousDifficulty)
            || LevelProgress.IsCompleted(category, requiredPreviousDifficulty);

        if (button != null) button.interactable = unlocked;
        if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked);

        bool completed = LevelProgress.IsCompleted(category, difficulty);

        if (cardImage != null)
        {
            Sprite target = completed ? completedSprite : placeholderSprite;
            if (target != null) cardImage.sprite = target;
        }

        int earned = completed ? LevelProgress.GetStars(category, difficulty) : 0;
        for (int i = 0; i < stars.Length; i++)
            if (stars[i] != null)
                stars[i].SetActive(i < earned);
    }
}
