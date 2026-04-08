using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public Transform spawnPoint;

    void Start()
    {
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
    }
}