using Unity.Netcode;
using UnityEngine;

public class Buff : NetworkBehaviour
{
    public float rotationSpeed = 50f;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}