using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One per level (on each SpawnPoint). All levels can share the SAME RewardPanel:
// every time ShowRewardScreen() runs, the banner, stars and reward text are
// fully reset for this level.
// Wire ShowRewardScreen() to the level checklist's "On All Steps Complete".
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

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip congratsSound;

    [Header("Positioning")]
    [Tooltip("Moves the panel in front of the player's view when it appears")]
    public bool positionInFrontOfPlayer = true;
    public float distanceFromPlayer = 1.5f;
    public float heightOffset = 0f;

    private bool hasShown = false;
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
            rewardPanel.SetActive(false);
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

        if (rewardPanel != null)
        {
            if (positionInFrontOfPlayer && Camera.main != null)
                PositionInFrontOfPlayer();
            rewardPanel.SetActive(true);
        }

        if (audioSource != null && congratsSound != null)
            audioSource.PlayOneShot(congratsSound);

        StartCoroutine(PlayRevealAnimation(stars));

        if (!string.IsNullOrEmpty(rewardPartName) && RewardManager.Instance != null)
            RewardManager.Instance.UnlockPart(rewardPartName);
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
    private void PositionInFrontOfPlayer()
    {
        Transform cam = Camera.main.transform;

        // Flatten forward so the panel doesn't tilt with head pitch
        Vector3 flatForward = cam.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
        flatForward.Normalize();

        Vector3 targetPos = cam.position + flatForward * distanceFromPlayer;
        targetPos.y = cam.position.y + heightOffset;
        rewardPanel.transform.position = targetPos;

        Vector3 lookDir = rewardPanel.transform.position - cam.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
            rewardPanel.transform.rotation = Quaternion.LookRotation(lookDir);
    }
}