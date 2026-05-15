using UnityEngine;

public class HardRamCleaner : MonoBehaviour
{
    public HardPCManager hardPCManager;
    public MeshRenderer ramRenderer;
    public Material dirtyMaterial;
    public Material cleanMaterial;
    public AudioSource cleanSound;

    private bool isClean = false;

    void Start()
    {
        if (ramRenderer != null && dirtyMaterial != null)
            ramRenderer.material = dirtyMaterial;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Eraser"))
            CleanRam();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Eraser"))
            CleanRam();
    }

    void CleanRam()
    {
        if (isClean) return;

        isClean = true;

        if (ramRenderer != null && cleanMaterial != null)
            ramRenderer.material = cleanMaterial;

        if (cleanSound != null)
            cleanSound.Play();

        if (hardPCManager != null)
            hardPCManager.BrokenRamCleaned();

        Debug.Log("HARD RAM CLEANED");
    }
}