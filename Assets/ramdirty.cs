using UnityEngine;

public class RAM : MonoBehaviour
{
    public PCManager pcManager;

    public MeshRenderer ramRenderer;
    public Material cleanMat;
    public Material dirtyMat;

    public Transform snapPoint;
    public float snapDistance = 0.05f;

    private bool isNearSlot = false;

    void Start()
    {
        // Start dirty
        ramRenderer.material = dirtyMat;
        pcManager.ramCleaned = false;
    }

    void Update()
    {
        // Check if near slot (for proper seating)
        float distance = Vector3.Distance(transform.position, snapPoint.position);

        if (distance < snapDistance)
        {
            transform.position = snapPoint.position;
            transform.rotation = snapPoint.rotation;

            pcManager.ramInserted = true;
            pcManager.CheckFix();
        }
        else
        {
            pcManager.ramInserted = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // CLEAN RAM
        if (other.CompareTag("Eraser"))
        {
            ramRenderer.material = cleanMat;
            pcManager.ramCleaned = true;

            Debug.Log("RAM CLEANED");
        }
    }
}