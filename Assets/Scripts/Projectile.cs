using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    public int damage = 5;
    public ulong ownerId;
    public Vector3 direction;
    public float speed = 10f;
    public float lifetime = 5f;

    private float spawnTime;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            spawnTime = Time.time;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        transform.position += direction * speed * Time.deltaTime;


        if (Time.time - spawnTime > lifetime)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent<Player>(out Player player))
        {
            // Don't damage the shooter
            if (player.OwnerClientId != ownerId)
            {
                player.TakeDamageServerRpc(damage, ownerId);
                GetComponent<NetworkObject>().Despawn();
            }
        }
        else if (!other.isTrigger) // Hit environment
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}