using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RAMMedium : MonoBehaviour
{
    public MeshRenderer ramRenderer;
    public Material dirtyMat;
    public Material cleanMat;

    public AudioSource cleanSound;

    public bool isClean = false;
    public bool isInserted = false;

    private Rigidbody rb;
    private XRGrabInteractable grab;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void Start()
    {
        ramRenderer.material = dirtyMat;

        rb.useGravity = false;
        rb.isKinematic = true;
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        isInserted = false;

        rb.isKinematic = false;
        rb.useGravity = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.useGravity = true;
        rb.isKinematic = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Eraser") && !isClean)
        {
            CleanRAM();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Eraser") && !isClean)
        {
            CleanRAM();
        }
    }

    void CleanRAM()
    {
        isClean = true;

        if (ramRenderer != null && cleanMat != null)
            ramRenderer.material = cleanMat;

        if (cleanSound != null)
            cleanSound.Play();

        Debug.Log("RAM CLEANED");
    }
}