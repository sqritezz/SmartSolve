using UnityEngine;

public class HardRamCleaner : MonoBehaviour
{
    public HardPCManager hardPCManager;

    public Material cleanMaterial;
    public Renderer ramRenderer;

    private bool cleaned = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (cleaned) return;

        if (collision.gameObject.CompareTag("Eraser"))
        {
            cleaned = true;

            if (ramRenderer != null && cleanMaterial != null)
                ramRenderer.material = cleanMaterial;

            if (hardPCManager != null)
                hardPCManager.BrokenRamCleaned();
        }
    }
}