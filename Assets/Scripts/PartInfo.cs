using UnityEngine;

// Attach this to any part (RAM, PSU, CPU, etc.) alongside its XR Grab Interactable.
// It just holds the label/description data that the hover panel will display.
public class PartInfo : MonoBehaviour
{
    [Header("Info shown on the hover panel")]
    public string partName = "RAM";

    [TextArea(2, 4)]
    public string description = "Random Access Memory - stores temporary data for active processes.";
}