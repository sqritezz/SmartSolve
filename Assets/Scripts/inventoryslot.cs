using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to EACH item Image on the inventory panel (one per reward).
// The Image also needs a Box Collider + XR Simple Interactable.
//
// LOCKED   (level not finished): image hidden (or black silhouette), grey label
// UNLOCKED (level finished):     image visible, green label. Grab the image ->
//                                the real 3D reward goes straight into your hand.
// PLACED   (snapped in its AssemblySlot): image faded + checkmark.
//
// If the item gets dropped somewhere, grabbing the image again brings it back.
public class InventorySlot : MonoBehaviour
{
    public enum LockedStyle { Hidden, Silhouette }

    [Header("Part")]
    [Tooltip("Must EXACTLY match the Reward Part Name in LevelCompleteManager")]
    public string partName;

    [Tooltip("The real 3D reward item in the Display Room (has AssemblyPiece). Set it INACTIVE in the Hierarchy.")]
    public AssemblyPiece piece;

    [Tooltip("Put the item straight into the player's hand. Turn off for big items (desk, chair) so they appear at the spawn point instead.")]
    public bool grabIntoHand = true;

    [Tooltip("Where the item appears when it can't go into the hand (e.g. on the floor near the panel)")]
    public Transform fallbackSpawnPoint;

    [Header("Visuals")]
    [Tooltip("Leave empty to use the Image on this object")]
    public Image itemImage;
    public LockedStyle lockedStyle = LockedStyle.Hidden;
    public Color silhouetteColor = new Color(0f, 0f, 0f, 0.85f);
    [Tooltip("Image tint once the item is placed in its spot")]
    public Color placedColor = new Color(1f, 1f, 1f, 0.35f);
    [Tooltip("Optional checkmark shown once the item is placed")]
    public GameObject placedCheckmark;

    [Header("Label (optional)")]
    public TMP_Text label;
    public Color lockedLabelColor = new Color(0.478f, 0.498f, 0.549f);    // #7A7F8C
    public Color collectedLabelColor = new Color(0.486f, 1f, 0.541f);     // #7CFF8A

    [Header("Audio (optional)")]
    public AudioSource takeSound;
    public AudioSource lockedSound;

    [Header("State (read-only)")]
    public bool isUnlocked = false;
    public bool isTaken = false;
    public bool isPlaced = false;

    private XRBaseInteractable slotInteractable;
    private XRGrabInteractable pieceGrab;
    private Rigidbody pieceRb;
    private bool busy = false;
    private float nextRefreshTime = 0f;

    private void Awake()
    {
        if (itemImage == null)
            itemImage = GetComponent<Image>();

        FitColliderToImage();

        slotInteractable = GetComponent<XRBaseInteractable>();
        if (slotInteractable != null)
            slotInteractable.selectEntered.AddListener(OnSlotSelected);

        if (piece != null)
        {
            pieceGrab = piece.GetComponent<XRGrabInteractable>();
            pieceRb = piece.GetComponent<Rigidbody>();

            // Items stay hidden until taken from the inventory
            if (!piece.isSnapped)
                piece.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        // Cheap polling so the slot updates as soon as a level unlocks the part
        // or the item gets snapped into place
        if (Time.time >= nextRefreshTime)
        {
            nextRefreshTime = Time.time + 0.25f;
            Refresh();
        }
    }

    // ---------- Visual state ----------
    public void Refresh()
    {
        isUnlocked = RewardManager.Instance != null && RewardManager.Instance.IsPartUnlocked(partName);
        isPlaced = piece != null && piece.isSnapped;

        if (itemImage != null)
        {
            if (!isUnlocked)
            {
                itemImage.enabled = lockedStyle == LockedStyle.Silhouette;
                itemImage.color = silhouetteColor;
            }
            else
            {
                itemImage.enabled = true;
                itemImage.color = isPlaced ? placedColor : Color.white;
            }
        }

        if (placedCheckmark != null)
            placedCheckmark.SetActive(isPlaced);

        if (label != null)
            label.color = isUnlocked ? collectedLabelColor : lockedLabelColor;
    }

    // ---------- Grabbing the image ----------
    private void OnSlotSelected(SelectEnterEventArgs args)
    {
        if (busy) return;
        Refresh();

        IXRSelectInteractor interactor = args.interactorObject;
        XRInteractionManager manager = args.manager;

        if (!isUnlocked)
        {
            if (lockedSound != null) lockedSound.Play();
            StartCoroutine(ReleaseSlotNextFrame(interactor, manager));
            return;
        }

        if (isPlaced || piece == null)
        {
            StartCoroutine(ReleaseSlotNextFrame(interactor, manager));
            return;
        }

        StartCoroutine(GiveItem(interactor, manager));
    }

    private IEnumerator ReleaseSlotNextFrame(IXRSelectInteractor interactor, XRInteractionManager manager)
    {
        yield return null;
        if (manager != null && interactor != null && slotInteractable != null && slotInteractable.isSelected)
            manager.SelectExit(interactor, slotInteractable);
    }

    private IEnumerator GiveItem(IXRSelectInteractor interactor, XRInteractionManager manager)
    {
        busy = true;

        // 1. Let go of the image itself
        yield return ReleaseSlotNextFrame(interactor, manager);
        yield return null;

        // 2. Show the real item
        piece.gameObject.SetActive(true);
        isTaken = true;

        bool handed = false;

        // 3. Try putting it straight into the hand that grabbed the image
        if (grabIntoHand && manager != null && interactor != null && pieceGrab != null && pieceGrab.enabled)
        {
            Transform attach = interactor.GetAttachTransform(pieceGrab);
            if (attach != null)
                MovePiece(attach.position);

            manager.SelectEnter(interactor, pieceGrab);
            handed = pieceGrab.isSelected;
        }

        // 4. Otherwise put it at the spawn point
        if (!handed)
        {
            if (fallbackSpawnPoint != null)
                MovePiece(fallbackSpawnPoint.position, fallbackSpawnPoint.rotation);
        }

        if (takeSound != null) takeSound.Play();

        busy = false;
        Refresh();
    }

    private void MovePiece(Vector3 position, Quaternion? rotation = null)
    {
        if (pieceRb != null && !pieceRb.isKinematic)
        {
            pieceRb.linearVelocity = Vector3.zero;
            pieceRb.angularVelocity = Vector3.zero;
        }

        piece.transform.position = position;
        if (rotation.HasValue) piece.transform.rotation = rotation.Value;

        if (pieceRb != null)
        {
            pieceRb.position = piece.transform.position;
            pieceRb.rotation = piece.transform.rotation;
        }
    }

    // Makes the Box Collider match the Image's size on the panel
    private void FitColliderToImage()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        RectTransform rect = transform as RectTransform;
        if (box == null || rect == null) return;

        box.center = new Vector3(rect.rect.center.x, rect.rect.center.y, 0f);
        box.size = new Vector3(rect.rect.width, rect.rect.height, 10f);
    }
}