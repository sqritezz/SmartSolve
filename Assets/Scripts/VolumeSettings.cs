using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    public static VolumeSettings Instance { get; private set; }

    public AudioMixer audioMixer;

    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Mixer Groups (drag from MainAudioMixer)")]
    [Tooltip("Optional. If left empty, the script finds the group named exactly 'Background'.")]
    public AudioMixerGroup bgGroup;
    [Tooltip("Optional. If left empty, the script finds the group named exactly 'SFX'.")]
    public AudioMixerGroup sfxGroup;

    [Header("Auto-routing")]
    [Tooltip("Moves Audio Sources with no output (or on Master) to SFX. Sources already on Background or SFX are left alone.")]
    public bool autoRouteToSFX = true;

    [Header("Debug")]
    public bool logRouting = false;

    // Exposed parameter names — must match the Audio Mixer's Exposed Parameters exactly
    const string MasterParam = "MasterVolume";
    const string BgmParam = "BGMVolume";
    const string SfxParam = "SFXVolume";

    const float MinValue = 0.0001f;

    private AudioMixerGroup masterGroup;

    void Awake()
    {
        Instance = this;

        if (audioMixer == null)
        {
            Debug.LogError("VolumeSettings: No Audio Mixer assigned.", this);
            return;
        }

        masterGroup = FindGroupExact("Master");
        if (bgGroup == null) bgGroup = FindGroupExact("Background");
        if (sfxGroup == null) sfxGroup = FindGroupExact("SFX");
    }

    void Start()
    {
        if (autoRouteToSFX) RouteAllAudioSources();

        SetupSlider(masterSlider, MasterParam);
        SetupSlider(bgmSlider, BgmParam);
        SetupSlider(sfxSlider, SfxParam);

        // Apply saved values to the mixer (SetFloat on the mixer doesn't work in Awake)
        if (masterSlider != null) SetMasterVolume(masterSlider.value);
        if (bgmSlider != null) SetBGMVolume(bgmSlider.value);
        if (sfxSlider != null) SetSFXVolume(sfxSlider.value);
    }

    void SetupSlider(Slider slider, string key)
    {
        if (slider == null) return;
        slider.minValue = MinValue;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(key, 1f));
    }

    // ---------- Slider functions (On Value Changed → Dynamic float) ----------

    public void SetMasterVolume(float value) => ApplyVolume(MasterParam, value);
    public void SetBGMVolume(float value) => ApplyVolume(BgmParam, value);
    public void SetSFXVolume(float value) => ApplyVolume(SfxParam, value);

    void ApplyVolume(string param, float value)
    {
        if (audioMixer == null) return;

        // SetFloat returns false if the exposed parameter name doesn't exist
        if (!audioMixer.SetFloat(param, ToDecibels(value)))
            Debug.LogWarning($"VolumeSettings: Exposed parameter '{param}' not found in {audioMixer.name}. Check the spelling in Exposed Parameters.", this);

        PlayerPrefs.SetFloat(param, value);
    }

    // ---------- Routing ----------

    // Moves every Audio Source (active or inactive) that has no output or is on Master to SFX.
    // Anything already on Background (the music) or SFX is left alone.
    public void RouteAllAudioSources()
    {
        if (sfxGroup == null) return;

        AudioSource[] sources = FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (AudioSource source in sources)
            RouteToSFX(source);
    }

    // Call this on Audio Sources you spawn at runtime (e.g. from prefabs).
    public void RouteToSFX(AudioSource source)
    {
        if (source == null || sfxGroup == null) return;

        AudioMixerGroup current = source.outputAudioMixerGroup;

        // Only reroute sources with no output, or ones sitting directly on Master
        bool unrouted = current == null || (masterGroup != null && current == masterGroup);
        if (!unrouted)
        {
            if (logRouting)
                Debug.Log($"[Routing] Kept '{source.name}' on {current.name}", source);
            return;
        }

        source.outputAudioMixerGroup = sfxGroup;
        if (logRouting)
            Debug.Log($"[Routing] Moved '{source.name}' to SFX", source);
    }

    // Use this INSTEAD of AudioSource.PlayClipAtPoint, which ignores the mixer.
    // Example: VolumeSettings.PlaySFX(errorClip, transform.position);
    public static void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        GameObject temp = new GameObject("SFX_" + clip.name);
        temp.transform.position = position;

        AudioSource source = temp.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f; // 3D sound for VR
        if (Instance != null) source.outputAudioMixerGroup = Instance.sfxGroup;

        source.Play();
        Destroy(temp, clip.length + 0.1f);
    }

    // ---------- Helpers ----------

    // Finds a group by its exact name (FindMatchingGroups alone can return the wrong group,
    // because it matches by path — e.g. "Master" also returns its children).
    AudioMixerGroup FindGroupExact(string groupName)
    {
        AudioMixerGroup[] groups = audioMixer.FindMatchingGroups(string.Empty);
        foreach (AudioMixerGroup g in groups)
        {
            if (g.name == groupName) return g;
        }

        Debug.LogWarning($"VolumeSettings: Mixer group '{groupName}' not found in {audioMixer.name}.", this);
        return null;
    }

    float ToDecibels(float value)
    {
        return Mathf.Log10(Mathf.Max(value, MinValue)) * 20f;
    }

    void OnDisable()
    {
        PlayerPrefs.Save();
    }
}