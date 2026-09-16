using UnityEngine;

// Attach this to the PSU FAN parent object (or anywhere convenient).
// Works across multiple mesh pieces by tinting their color, instead of
// swapping whole Material assets -- no need to create matching dirty/clean
// materials for every individual fan piece.
public class PSUFanCleaner : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Every mesh piece that makes up the fan (drag all of them in: blades, rings, etc.)")]
    public MeshRenderer[] fanRenderers;

    [Tooltip("Dusty tint color applied when dirty")]
    public Color dirtyColor = new Color(0.55f, 0.5f, 0.4f);

    [Tooltip("Normal color when clean (usually plain white = no tint, shows original material color)")]
    public Color cleanColor = Color.white;

    public AudioSource cleanSound;

    [Header("State")]
    public bool isClean = false;

    [Header("Eraser Detection")]
    public string eraserTag = "Eraser";

    [Header("Power Button Link")]
    public PSUPowerButton powerButton;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex = 1;
    public int objectiveIndex = 0;

    private void Start()
    {
        isClean = false;
        ApplyColor();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryClean(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryClean(other);
    }

    private void TryClean(Collider other)
    {
        if (isClean) return;
        if (!other.CompareTag(eraserTag)) return;

        isClean = true;
        ApplyColor();

        if (cleanSound != null)
            cleanSound.Play();

        if (powerButton != null)
            powerButton.isPsuFixed = true;

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }

    private void ApplyColor()
    {
        if (fanRenderers == null) return;

        Color target = isClean ? cleanColor : dirtyColor;

        foreach (var renderer in fanRenderers)
        {
            if (renderer != null)
                renderer.material.color = target;
        }
    }
}