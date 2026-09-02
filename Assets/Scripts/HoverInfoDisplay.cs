using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class HoverInfoDisplay : MonoBehaviour
{
    public GameObject infoPanel;
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    public GameObject info;

    public float verticalOffset = 0.15f;

    private Transform hoveredPart;

    private void Awake()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    private void OnEnable()
    {
        var interactables = FindObjectsByType<XRBaseInteractable>(FindObjectsSortMode.None);
        foreach (var interactable in interactables)
        {
            if (interactable.GetComponent<PartInfo>() != null)
            {
                interactable.hoverEntered.AddListener(OnHoverEntered);
                interactable.hoverExited.AddListener(OnHoverExited);
            }
        }
    }

    private void OnDisable()
    {
        var interactables = FindObjectsByType<XRBaseInteractable>(FindObjectsSortMode.None);
        foreach (var interactable in interactables)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        var info = args.interactableObject.transform.GetComponent<PartInfo>();
        if (info == null) return;

        hoveredPart = args.interactableObject.transform;

        if (titleText != null) titleText.text = info.partName;
        if (descriptionText != null) descriptionText.text = info.description;

        if (infoPanel != null)
            infoPanel.SetActive(true);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (args.interactableObject.transform == hoveredPart)
        {
            hoveredPart = null;
            if (infoPanel != null)
                infoPanel.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (hoveredPart == null || infoPanel == null) return;

        // Float the panel above the part and make it face the camera/player.
        Vector3 targetPos = hoveredPart.position + Vector3.up * verticalOffset;
        infoPanel.transform.position = targetPos;

        if (Camera.main != null)
        {
            infoPanel.transform.rotation = Quaternion.LookRotation(
                infoPanel.transform.position - Camera.main.transform.position);
        }
    }
}