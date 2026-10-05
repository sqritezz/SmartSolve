using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// One per reward item. Put it on an EMPTY GameObject (scale 1, rotation 0),
// add a Box Collider, then drag the item from your arranged PC set into
// "Blueprint Piece".
//
// On game start:
//  - the item's current position/rotation becomes the snap spot
//  - the trigger collider resizes to fit around it
//  - a see-through GHOST copy is created in that spot
//  - the real item is hidden (it comes back from the inventory panel)
// When the player releases the matching item near the ghost, it snaps in,
// locks, and the ghost disappears.
[RequireComponent(typeof(Collider))]
public class AssemblySlot : MonoBehaviour
{
    [Header("Part")]
    [Tooltip("Must match AssemblyPiece.partName. Left empty = copied from Blueprint Piece.")]
    public string expectedPartName;

    [Header("Blueprint (recommended)")]
    [Tooltip("The real item already placed in your PC set. Its current spot becomes the snap point and its shape becomes the ghost.")]
    public AssemblyPiece blueprintPiece;
    [Tooltip("Hide the real item when the game starts")]
    public bool hideBlueprintPieceOnStart = true;
    [Tooltip("Resize this object's Box Collider to fit around the item")]
    public bool autoFitTrigger = true;
    [Tooltip("Extra space (meters) around the item where releasing it still snaps")]
    public float triggerPadding = 0.15f;

    [Header("Manual Snap Point (only if no Blueprint Piece)")]
    public Transform snapPoint;

    [Header("Order")]
    [Tooltip("These slots must be filled first (e.g. the Desk slot for the Monitor/Keyboard/Mouse)")]
    public AssemblySlot[] requiresFilledFirst;

    [Header("Ghost Preview")]
    public bool showGhost = true;
    [Tooltip("A transparent material (see setup notes)")]
    public Material ghostMaterial;
    public Color ghostColor = new Color(0.3f, 0.8f, 1f, 0.25f);
    [Tooltip("Ghost color while the player is holding this item")]
    public Color ghostHighlightColor = new Color(0.3f, 1f, 0.5f, 0.55f);
    [Tooltip("Only show the ghost after the part is unlocked")]
    public bool onlyShowWhenUnlocked = false;
    [Tooltip("Ghost gently pulses so it's easy to notice")]
    public bool pulse = true;

    [Header("Audio")]
    public AudioSource snapSound;

    [Header("Events")]
    public UnityEvent onPieceSnapped;

    [Header("State (read-only)")]
    public bool isFilled = false;

    public bool IsFilled => isFilled;

    private GameObject ghostRoot;
    private readonly List<Renderer> ghostRenderers = new List<Renderer>();
    private MaterialPropertyBlock propertyBlock;
    private XRGrabInteractable blueprintGrab;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        propertyBlock = new MaterialPropertyBlock();

        if (blueprintPiece != null)
        {
            if (string.IsNullOrEmpty(expectedPartName))
                expectedPartName = blueprintPiece.partName;

            blueprintGrab = blueprintPiece.GetComponent<XRGrabInteractable>();

            CreateSnapPointFromPiece();
            if (autoFitTrigger) FitTriggerToPiece();
            if (showGhost) BuildGhost();

            if (hideBlueprintPieceOnStart && !blueprintPiece.isSnapped)
                blueprintPiece.gameObject.SetActive(false);
        }
        else if (snapPoint == null)
        {
            snapPoint = transform;
        }
    }

    private void Update()
    {
        if (ghostRoot == null) return;

        bool unlocked = !onlyShowWhenUnlocked ||
                        (RewardManager.Instance != null && RewardManager.Instance.IsPartUnlocked(expectedPartName));

        bool visible = showGhost && !isFilled && unlocked && RequirementsMet();
        if (ghostRoot.activeSelf != visible)
            ghostRoot.SetActive(visible);

        if (!visible) return;

        bool holding = blueprintGrab != null && blueprintGrab.isActiveAndEnabled && blueprintGrab.isSelected;
        Color c = holding ? ghostHighlightColor : ghostColor;

        if (pulse)
            c.a *= 0.75f + 0.25f * Mathf.Sin(Time.time * 3f);

        propertyBlock.SetColor("_BaseColor", c); // URP
        propertyBlock.SetColor("_Color", c);     // Built-in
        foreach (var r in ghostRenderers)
            if (r != null) r.SetPropertyBlock(propertyBlock);
    }

    // ---------- Snapping ----------
    private void OnTriggerStay(Collider other)
    {
        if (isFilled || !RequirementsMet()) return;

        AssemblyPiece piece = other.GetComponentInParent<AssemblyPiece>();
        if (piece == null || piece.partName != expectedPartName || piece.isSnapped) return;

        // Only snap once the player has let go of it (not mid-grab)
        XRGrabInteractable grab = piece.GetComponent<XRGrabInteractable>();
        if (grab != null && grab.isSelected) return;

        SnapPiece(piece, grab);
    }

    private void SnapPiece(AssemblyPiece piece, XRGrabInteractable grab)
    {
        isFilled = true;

        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

        piece.transform.position = snapPoint.position;
        piece.transform.rotation = snapPoint.rotation;

        // Lock it in place -- no more picking it up once it's correctly assembled
        if (grab != null)
            grab.enabled = false;

        if (ghostRoot != null) ghostRoot.SetActive(false);
        if (snapSound != null) snapSound.Play();

        piece.OnSnapped();
        onPieceSnapped?.Invoke();
    }

    private bool RequirementsMet()
    {
        if (requiresFilledFirst == null) return true;
        foreach (var slot in requiresFilledFirst)
            if (slot != null && !slot.isFilled) return false;
        return true;
    }

    // ---------- Blueprint setup ----------
    private void CreateSnapPointFromPiece()
    {
        Transform p = blueprintPiece.transform;
        GameObject anchor = new GameObject(expectedPartName + "_SnapPoint");
        anchor.transform.SetPositionAndRotation(p.position, p.rotation);
        anchor.transform.SetParent(transform, true);
        snapPoint = anchor.transform;
    }

    // Works even if the item is already inactive (uses mesh data, not renderer bounds)
    private void FitTriggerToPiece()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        bool hasBounds = false;
        Bounds worldBounds = new Bounds();

        foreach (var mf in blueprintPiece.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null)
                Encapsulate(ref worldBounds, ref hasBounds, mf.sharedMesh.bounds, mf.transform);

        foreach (var smr in blueprintPiece.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.sharedMesh != null)
                Encapsulate(ref worldBounds, ref hasBounds, smr.sharedMesh.bounds, smr.transform);

        if (!hasBounds) return;

        worldBounds.Expand(triggerPadding * 2f);

        Vector3 scale = transform.lossyScale;
        box.center = transform.InverseTransformPoint(worldBounds.center);
        box.size = new Vector3(
            worldBounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            worldBounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
            worldBounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
    }

    private static void Encapsulate(ref Bounds world, ref bool hasBounds, Bounds local, Transform t)
    {
        Vector3 c = local.center;
        Vector3 e = local.extents;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = c + new Vector3(
                (i & 1) == 0 ? -e.x : e.x,
                (i & 2) == 0 ? -e.y : e.y,
                (i & 4) == 0 ? -e.z : e.z);
            Vector3 worldPoint = t.TransformPoint(corner);

            if (!hasBounds) { world = new Bounds(worldPoint, Vector3.zero); hasBounds = true; }
            else world.Encapsulate(worldPoint);
        }
    }

    private void BuildGhost()
    {
        if (ghostMaterial == null)
        {
            Debug.LogWarning(name + ": no Ghost Material assigned, ghost preview skipped.");
            return;
        }

        ghostRoot = new GameObject(expectedPartName + "_Ghost");
        ghostRoot.transform.SetParent(transform, false);

        foreach (var mf in blueprintPiece.GetComponentsInChildren<MeshFilter>(true))
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr != null && mf.sharedMesh != null)
                AddGhostPart(mf.sharedMesh, mf.transform, mr.sharedMaterials.Length);
        }

        foreach (var smr in blueprintPiece.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.sharedMesh != null)
                AddGhostPart(smr.sharedMesh, smr.transform, smr.sharedMaterials.Length);
    }

    private void AddGhostPart(Mesh mesh, Transform source, int materialCount)
    {
        GameObject part = new GameObject("Ghost_" + source.name);
        part.transform.SetPositionAndRotation(source.position, source.rotation);
        part.transform.localScale = source.lossyScale;
        part.transform.SetParent(ghostRoot.transform, true);

        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = part.AddComponent<MeshRenderer>();

        Material[] mats = new Material[Mathf.Max(1, materialCount)];
        for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
        r.sharedMaterials = mats;

        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        ghostRenderers.Add(r);
    }
}