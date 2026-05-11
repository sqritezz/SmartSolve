using UnityEngine;

public class RamCleaner : MonoBehaviour
{
    public MeshRenderer ramRenderer;      // RAM mesh
    public Material dirtyMaterial;        // Dirty brown RAM
    public Material cleanMaterial;        // Clean RAM
    public bool isClean = false;
    public AudioSource cleanSound;

    private void Start()
    {
        if (ramRenderer != null && dirtyMaterial != null)
        {
            ramRenderer.material = dirtyMaterial;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Eraser") && !isClean)
        {
            CleanRAM();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Eraser") && !isClean)
        {
            CleanRAM();
        }
    }

    void CleanRAM()
    {
        isClean = true;

        if (ramRenderer != null && cleanMaterial != null)
        {
            ramRenderer.material = cleanMaterial;
        }

        if (cleanSound != null)
        {
            cleanSound.Play();
        }

        Debug.Log("RAM cleaned successfully!");
    }
}