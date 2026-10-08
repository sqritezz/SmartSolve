using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// One per level (on each SpawnPoint). All levels can share the SAME RewardPanel:
// every time ShowRewardScreen() runs, the banner, stars and reward text are
// fully reset for this level.
// Wire ShowRewardScreen() to the level checklist's "On All Steps Complete".
// The shared panel's NEXT and BACK TO MENU buttons are re-wired by whichever
// level just finished, so each level's Next goes to the right place.
// IMPORTANT: leave the buttons' own "On Click ()" lists EMPTY in the Inspector.
public class LevelCompleteManager : MonoBehaviour
{
    [Header("Reward Panel")]
    public GameObject rewardPanel;

    [Header("Congrats Banner (image)")]
    [Tooltip("The CONGRATULATIONS banner image (congratsTEXT)")]
    public GameObject congratsBanner;
    [Tooltip("Banner grows in with a little bounce when the panel appears")]
    public bool popInBanner = true;

    [Header("Stars (2D Images)")]
    [Tooltip("The 3 star Images on the panel, left to right")]
    public Image[] starImages;
    public Color earnedStarColor = Color.white;
    public Color unearnedStarColor = new Color(0.25f, 0.25f, 0.25f, 0.7f);
    [Tooltip("Hide unearned stars completely instead of showing them grey")]
    public bool hideUnearnedStars = false;
    [Tooltip("Seconds between each earned star popping in")]
    public float starPopDelay = 0.35f;
    [Tooltip("Optional: plays once per earned star")]
    public AudioSource starSound;

    [Header("Scoring")]
    [Tooltip("This level's tracker. If empty, uses the PerformanceTracker on this same object, or Stars Earned below.")]
    public PerformanceTracker performanceTracker;
    [Tooltip("Fallback only - used when there is no PerformanceTracker")]
    [Range(0, 3)] public int starsEarned = 3;

    [Header("Reward")]
    [Tooltip("Must match RewardManager / AssemblyPiece names, e.g. Monitor, Mouse, Keyboard")]
    public string rewardPartName;
    [Tooltip("The 'rewards' text on the panel")]
    public TextMeshProUGUI rewardText;
    [Tooltip("{0} is replaced by the part name")]
    public string rewardTextFormat = "UNLOCKED: {0}";
    [Tooltip("Optional: an Image on the panel that shows the unlocked part")]
    public Image rewardIcon;
    public Sprite rewardSprite;

    [Header("Panel Buttons (this level)")]
    [Tooltip("The panel's NEXT button (shared by all levels)")]
    public Button nextButton;
    [Tooltip("What NEXT does for THIS level, e.g. TeleportToPoint.TeleportMedium. The panel closes by itself.")]
    public UnityEvent onNext;
    [Tooltip("The panel's BACK TO MAIN MENU button (shared by all levels)")]
    public Button backToMenuButton;
    [Tooltip("What BACK TO MENU does for THIS level. The panel closes by itself.")]
    public UnityEvent onBackToMenu;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip congratsSound;

    [Header("Positioning (panel follows the player's view)")]
    [Tooltip("Keeps the panel in front of the player's eyes while it's open")]
    public bool positionInFrontOfPlayer = true;
    [Tooltip("Meters in front of the eyes. Keep it close so nothing gets between you and the panel.")]
    public float distanceFromPlayer = 0.7f;
    public float heightOffset = 0f;
    [Tooltip("How quickly it catches up when you turn your head (higher = snappier)")]
    public float followSmoothing = 8f;
    [Tooltip("Draws the panel above other world UI (like the objectives board)")]
    public int sortingOrder = 100;

    private bool hasShown = false;
    private bool isShowing = false;
    private Vector3 bannerOriginalScale = Vector3.one;
    private Vector3[] starOriginalScales;

    private void Awake()
    {
        if (performanceTracker == null)
            performanceTracker = GetComponent<PerformanceTracker>();

        if (congratsBanner != null)
            bannerOriginalScale = congratsBanner.transform.localScale;

        if (starImages != null)
        {
            starOriginalScales = new Vector3[starImages.Length];
            for (int i = 0; i < starImages.Length; i++)
                if (starImages[i] != null)
                    starOriginalScales[i] = starImages[i].transform.localScale;
        }

        if (rewardPanel != null)
        {
            Canvas canvas = rewardPanel.GetComponentInParent<Canvas>(true);
            if (canvas == null) canvas = rewardPanel.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = sortingOrder;
            }

            // Make sure the panel is fully opaque
            CanvasGroup group = rewardPanel.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1f;

            rewardPanel.SetActive(false);
        }
    }

    // Keep the panel in front of the player's eyes while this level's panel is open
    private void LateUpdate()
    {
        if (!isShowing || rewardPanel == null || !rewardPanel.activeInHierarchy) { isShowing = false; return; }
        if (!positionInFrontOfPlayer || Camera.main == null) return;

        GetTargetPose(out Vector3 pos, out Quaternion rot);
        float k = 1f - Mathf.Exp(-followSmoothing * Time.unscaledDeltaTime);
        rewardPanel.transform.position = Vector3.Lerp(rewardPanel.transform.position, pos, k);
        rewardPanel.transform.rotation = Quaternion.Slerp(rewardPanel.transform.rotation, rot, k);
    }

    // Call this from the checklist's "On All Steps Complete" event
    public void ShowRewardScreen()
    {
        if (hasShown) return; // never fire twice for the same level
        hasShown = true;

        int stars = performanceTracker != null ? performanceTracker.CalculateStars() : starsEarned;
        stars = Mathf.Clamp(stars, 0, starImages != null ? starImages.Length : 3);

        // Reward text + icon
        if (rewardText != null && !string.IsNullOrEmpty(rewardPartName))
            rewardText.text = string.Format(rewardTextFormat, rewardPartName);

        if (rewardIcon != null)
        {
            rewardIcon.gameObject.SetActive(rewardSprite != null);
            if (rewardSprite != null) rewardIcon.sprite = rewardSprite;
        }

        // Reset all stars to "unearned" before showing the panel
        ResetStars();

        // Point the shared buttons at THIS level
        WireButtons();

        if (rewardPanel != null)
        {
            if (positionInFrontOfPlayer && Camera.main != null)
            {
                GetTargetPose(out Vector3 pos, out Quaternion rot);
                rewardPanel.transform.SetPositionAndRotation(pos, rot); // appear right away
            }
            rewardPanel.SetActive(true);
            isShowing = true;
        }

        if (audioSource != null && congratsSound != null)
            audioSource.PlayOneShot(congratsSound);

        StartCoroutine(PlayRevealAnimation(stars));

        if (!string.IsNullOrEmpty(rewardPartName) && RewardManager.Instance != null)
            RewardManager.Instance.UnlockPart(rewardPartName);
    }

    // ---------- Buttons ----------
    private void WireButtons()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => { ClosePanel(); onNext?.Invoke(); });
        }
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(() => { ClosePanel(); onBackToMenu?.Invoke(); });
        }
    }

    public void ClosePanel()
    {
        isShowing = false;
        if (rewardPanel != null) rewardPanel.SetActive(false);
    }

    // ---------- Animation ----------
    private IEnumerator PlayRevealAnimation(int stars)
    {
        if (congratsBanner != null)
        {
            congratsBanner.SetActive(true);
            if (popInBanner)
                yield return PopScale(congratsBanner.transform, bannerOriginalScale, 0.35f);
        }

        for (int i = 0; i < stars; i++)
        {
            yield return new WaitForSecondsRealtime(starPopDelay);

            Image star = starImages[i];
            if (star == null) continue;

            star.gameObject.SetActive(true);
            star.color = earnedStarColor;
            if (starSound != null) starSound.Play();

            Vector3 original = starOriginalScales != null && i < starOriginalScales.Length
                ? starOriginalScales[i] : Vector3.one;
            yield return PopScale(star.transform, original, 0.25f);
        }
    }

    // Grows from 0 to 120% then settles at the original size
    private IEnumerator PopScale(Transform target, Vector3 originalScale, float duration)
    {
        float half = duration * 0.6f;
        float t = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(Vector3.zero, originalScale * 1.2f, t / half);
            yield return null;
        }

        t = 0f;
        float rest = duration - half;
        while (t < rest)
        {
            t += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, t / rest);
            yield return null;
        }

        target.localScale = originalScale;
    }

    private void ResetStars()
    {
        if (starImages == null) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            Image star = starImages[i];
            if (star == null) continue;

            if (starOriginalScales != null && i < starOriginalScales.Length)
                star.transform.localScale = starOriginalScales[i];

            star.color = unearnedStarColor;
            star.gameObject.SetActive(!hideUnearnedStars);
        }

        if (congratsBanner != null && popInBanner)
            congratsBanner.transform.localScale = Vector3.zero;
    }

    // ---------- Positioning ----------
    // Straight in front of the eyes, facing the player (not tilted, not flattened)
    private void GetTargetPose(out Vector3 pos, out Quaternion rot)
    {
        Transform cam = Camera.main.transform;
        pos = cam.position + cam.forward * distanceFromPlayer + Vector3.up * heightOffset;
        rot = Quaternion.LookRotation(pos - cam.position, Vector3.up);
    }
}