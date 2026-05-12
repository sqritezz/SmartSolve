using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RAM : MonoBehaviour
{
    public PCManager pcManager;

    public MeshRenderer ramRenderer;
    public Material cleanMat;
    public Material dirtyMat;

    public Transform snapPoint;
    public float snapDistance = 0.05f;

    private XRGrabInteractable grab;
    private Rigidbody rb;

    private bool isClean = false;
    private bool isInserted = false;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        ramRenderer.material = dirtyMat;

        pcManager.ramCleaned = false;
        pcManager.ramInserted = false;
    }

    void Update()
    {
        if (grab != null && grab.isSelected)
            return;

        float distance = Vector3.Distance(transform.position, snapPoint.position);

        if (distance < snapDistance && !isInserted)
        {
            SnapToSlot();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Eraser") && !isClean)
        {
            isClean = true;
            ramRenderer.material = cleanMat;
            pcManager.ramCleaned = true;

            Debug.Log("RAM CLEANED");
            pcManager.CheckFix();
        }
    }

    void SnapToSlot()
    {
        isInserted = true;

        transform.SetParent(null, true);
        transform.position = snapPoint.position;
        transform.rotation = snapPoint.rotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        pcManager.ramInserted = true;
        pcManager.CheckFix();

        Debug.Log("RAM INSERTED");
    }
}