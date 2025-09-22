using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    public int damage = 5;
    public ulong ownerId;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null && player.OwnerClientId != ownerId)
            {
                player.TakeDamageServerRpc(damage, ownerId);
                GetComponent<NetworkObject>().Despawn();
            }
        }
        else if (!other.CompareTag("Buff"))
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}