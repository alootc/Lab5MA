using Unity.Netcode;
using UnityEngine;

public class Buff : NetworkBehaviour
{
    public float rotationSpeed = 30f;

    private void Update()
    {
        
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}