using UnityEngine;

public class DisableTracer : MonoBehaviour
{
    void OnDisable()
    {
        Debug.Log("[DisableTracer] " + name + " was turned OFF by:\n" + System.Environment.StackTrace);
    }
}