using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

// Shows one shared info panel for whatever part (with PartInfo) the ray hovers.
// The panel floats just ABOVE the part. If something is in the way there
// (the PC case, a wall, a desk...), it moves to the RIGHT side, then the LEFT
// side (as seen by the player), then toward the player - first clear spot wins.
public class HoverInfoDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("World-space Canvas panel with a title and description text")]
    public GameObject infoPanel;
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    [Header("Positioning")]
    [Tooltip("Gap between the part and the panel (meters)")]
    public float gap = 0.05f;
    [Tooltip("Layers that can block the panel")]
    public LayerMask obstacleLayers = ~0;
    [Tooltip("Optional: the XR Origin, so the player's own hands/body never count as blocking")]
    public Transform ignoreRoot;
    [Tooltip("Show the checked spots in the Scene view while playing")]
    public bool debugDraw = false;

    private Transform hoveredPart;
    private RectTransform panelRect;
    private int currentSpot = -1;

    private readonly Vector3[] corners = new Vector3[4];
    private readonly Collider[] hits = new Collider[16];

    private void Awake()
    {
        if (infoPanel != null)
        {
            panelRect = infoPanel.GetComponent<RectTransform>();
            if (panelRect == null) panelRect = infoPanel.GetComponentInChildren<RectTransform>();
            infoPanel.SetActive(false);
        }
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
        currentSpot = -1; // pick a fresh spot

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
            currentSpot = -1;
            if (infoPanel != null)
                infoPanel.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (hoveredPart == null || infoPanel == null || Camera.main == null) return;

        Transform cam = Camera.main.transform;
        Bounds b = GetPartBounds(hoveredPart);
        Vector2 half = GetPanelHalfSize();

        // Directions as seen by the player
        Vector3 toCam = cam.position - b.center;
        toCam.y = 0f;
        if (toCam.sqrMagnitude < 0.0001f) toCam = -cam.forward;
        toCam.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, toCam).normalized; // player's right... flipped below
        right = -right;

        float sideReach = Mathf.Max(b.extents.x, b.extents.z);

        Vector3[] spots =
        {
            // 0: above
            b.center + Vector3.up * (b.extents.y + gap + half.y),
            // 1: right side (player's view), at the top half of the part
            b.center + right * (sideReach + gap + half.x) + Vector3.up * (b.extents.y * 0.5f),
            // 2: left side
            b.center - right * (sideReach + gap + half.x) + Vector3.up * (b.extents.y * 0.5f),
            // 3: in front, toward the player
            b.center + toCam * (sideReach + gap + 0.02f) + Vector3.up * (b.extents.y * 0.5f),
        };

        // Keep the current spot while it stays clear (no jumping around)
        if (currentSpot < 0 || IsBlocked(spots[currentSpot], half, cam))
        {
            currentSpot = spots.Length - 1; // fallback: toward the player
            for (int i = 0; i < spots.Length; i++)
            {
                if (!IsBlocked(spots[i], half, cam)) { currentSpot = i; break; }
            }
        }

        Vector3 pos = spots[currentSpot];
        infoPanel.transform.position = pos;
        infoPanel.transform.rotation = Quaternion.LookRotation(pos - cam.position);

        if (debugDraw)
        {
            for (int i = 0; i < spots.Length; i++)
                Debug.DrawLine(b.center, spots[i], i == currentSpot ? Color.green : Color.red);
        }
    }

    private bool IsBlocked(Vector3 pos, Vector2 half, Transform cam)
    {
        Quaternion rot = Quaternion.LookRotation(pos - cam.position);
        Vector3 halfExtents = new Vector3(half.x, half.y, 0.02f);

        int count = Physics.OverlapBoxNonAlloc(pos, halfExtents, hits, rot,
                                               obstacleLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Transform t = hits[i].transform;
            if (t.IsChildOf(infoPanel.transform)) continue;          // the panel itself
            if (ignoreRoot != null && t.IsChildOf(ignoreRoot)) continue; // the player
            return true;
        }
        return false;
    }

    // Real size of the part (all its meshes), not just its pivot point
    private Bounds GetPartBounds(Transform part)
    {
        bool found = false;
        Bounds b = new Bounds(part.position, Vector3.zero);

        foreach (var r in part.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (!found) { b = r.bounds; found = true; }
            else b.Encapsulate(r.bounds);
        }

        if (!found)
        {
            foreach (var c in part.GetComponentsInChildren<Collider>())
            {
                if (!found) { b = c.bounds; found = true; }
                else b.Encapsulate(c.bounds);
            }
        }

        return b;
    }

    // Half width/height of the panel in meters (from the canvas size and scale)
    private Vector2 GetPanelHalfSize()
    {
        if (panelRect == null) return new Vector2(0.15f, 0.08f);

        panelRect.GetWorldCorners(corners);
        float w = Vector3.Distance(corners[0], corners[3]);
        float h = Vector3.Distance(corners[0], corners[1]);
        return new Vector2(w * 0.5f, h * 0.5f);
    }
}