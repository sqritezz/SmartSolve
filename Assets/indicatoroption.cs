using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Options menu: "Indicators: ON / OFF" for the REAL scenes.
// (Tutorials always keep their indicators.)
//
// Works with EITHER:
//   - a Unity UI Toggle  -> drag it into "Toggle"  (nothing else to wire), or
//   - a normal Button    -> its On Click -> IndicatorOption.Flip
// It uses the same answer as the "How well do you know computer?" panel:
//   "I'm still learning" = ON, "I know computers" = OFF. Either can change the other.
public class IndicatorOption : MonoBehaviour
{
    [Tooltip("Optional: a UI Toggle (checkbox)")]
    public Toggle toggle;
    [Tooltip("Optional: text that shows the current state")]
    public TMP_Text stateText;

    public string onText = "Indicators: ON";
    public string offText = "Indicators: OFF";
    public string onTextFilipino = "Mga Indicator: BUKAS";
    public string offTextFilipino = "Mga Indicator: SARADO";

    private bool updating;

    private static bool IsOn => !ExperienceChoice.IsExperienced;

    private void OnEnable()
    {
        ExperienceChoice.OnChanged += Refresh;
        LanguageManager.OnLanguageChanged += Refresh;
        if (toggle != null) toggle.onValueChanged.AddListener(OnToggle);
        Refresh();
    }

    private void OnDisable()
    {
        ExperienceChoice.OnChanged -= Refresh;
        LanguageManager.OnLanguageChanged -= Refresh;
        if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggle);
    }

    // For a normal button
    public void Flip() { ExperienceChoice.SetIndicatorsInRealScene(!IsOn); }

    // Optional: separate ON / OFF buttons
    public void TurnOn() { ExperienceChoice.SetIndicatorsInRealScene(true); }
    public void TurnOff() { ExperienceChoice.SetIndicatorsInRealScene(false); }

    private void OnToggle(bool on)
    {
        if (updating) return;
        ExperienceChoice.SetIndicatorsInRealScene(on);
    }

    private void Refresh()
    {
        updating = true;
        if (toggle != null) toggle.isOn = IsOn;
        updating = false;

        if (stateText != null)
            stateText.text = IsOn ? LanguageManager.Pick(onText, onTextFilipino)
                                  : LanguageManager.Pick(offText, offTextFilipino);
    }
}