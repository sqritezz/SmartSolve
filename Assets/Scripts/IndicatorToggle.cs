using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach to the Toggle object inside IndicatorRow.
// Makes a sliding on/off switch, shows/hides the indicator, and saves the choice.
public class IndicatorToggle : MonoBehaviour
{
    [Header("Toggle parts")]
    public Toggle toggle;            // the Toggle component
    public RectTransform knob;       // the round knob (old "Checkmark")
    public Image background;         // the track behind the knob
    public TMP_Text stateLabel;      // optional "On"/"Off" text

    [Header("Look")]
    public Color onColor  = new Color32(239, 159, 39, 255); // gold
    public Color offColor = new Color32(99, 56, 6, 255);    // dark brown
    [Tooltip("Optional: if set, these sprites are swapped instead of tinting colors")]
    public Sprite trackOnSprite, trackOffSprite, knobOnSprite, knobOffSprite;
    public float knobOnX  = 16f;     // knob X when ON  (right side)
    public float knobOffX = -16f;    // knob X when OFF (left side)
    public float slideSpeed = 12f;

    [Header("What the indicator controls")]
    public GameObject[] indicatorObjects; // hint arrows, hover markers, etc.

    public const string PrefKey = "IndicatorOn";
    float targetX;

    void Start()
    {
        bool on = PlayerPrefs.GetInt(PrefKey, 1) == 1;   // default ON
        toggle.SetIsOnWithoutNotify(on);
        Apply(on, true);
        toggle.onValueChanged.AddListener(value => Apply(value, false));
    }

    void Apply(bool on, bool instant)
    {
        targetX = on ? knobOnX : knobOffX;
        if (instant)
            knob.anchoredPosition = new Vector2(targetX, knob.anchoredPosition.y);

        if (trackOnSprite != null && trackOffSprite != null)
        {
            background.sprite = on ? trackOnSprite : trackOffSprite;
            background.color = Color.white;
        }
        else
        {
            background.color = on ? onColor : offColor;
        }

        Image knobImage = knob.GetComponent<Image>();
        if (knobImage != null && knobOnSprite != null && knobOffSprite != null)
            knobImage.sprite = on ? knobOnSprite : knobOffSprite;
        if (stateLabel != null) stateLabel.text = on ? "On" : "Off";

        foreach (var obj in indicatorObjects)
            if (obj != null) obj.SetActive(on);

        PlayerPrefs.SetInt(PrefKey, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    void Update()
    {
        // Smoothly slide the knob (unscaled so it works while the game is paused)
        Vector2 p = knob.anchoredPosition;
        p.x = Mathf.Lerp(p.x, targetX, Time.unscaledDeltaTime * slideSpeed);
        knob.anchoredPosition = p;
    }

    // Other scripts (e.g. hint spawners) can check this before showing a hint
    public static bool IsIndicatorOn => PlayerPrefs.GetInt(PrefKey, 1) == 1;
}
