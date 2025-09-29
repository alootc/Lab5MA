//using Cinemachine;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class Player : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> accoundID = new();
    public NetworkVariable<int> health = new(100);
    public NetworkVariable<int> attack = new(5);

    public float moveSpeed = 5f;
    public float projectileSpeed = 10f;
    public GameObject projectileSpawnPoint;

    private CharacterController controller;
    private CinemachineCamera virtualCamera;

    [System.Serializable]
    public class PlayerData
    {
        public string accoundID;
        public Vector3 position;
        public int health;
        public int attack;

        public PlayerData(string id, Vector3 pos, int hp, int atk)
        {
            accoundID = id;
            position = pos;
            health = hp;
            attack = atk;
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SetupCamera();
        }


    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= OnHealthChanged;

        if (IsServer)
        {
            GameManager.Instance.playerStatesByAccountID[accoundID.Value.ToString()] =
                new PlayerData(accoundID.Value.ToString(), transform.position, health.Value, attack.Value);
        }

        print("Me desconecté " + NetworkManager.Singleton.LocalClientId + " y se guardó la data de " + accoundID.Value.ToString());
    }

    private void SetupCamera()
    {
        virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        if (virtualCamera != null)
        {
            virtualCamera.Follow = transform;
            virtualCamera.LookAt = transform;
        }
    }

    public void SetData(PlayerData playerData)
    {
        accoundID.Value = playerData.accoundID;
        health.Value = playerData.health;
        attack.Value = playerData.attack;
        transform.position = playerData.position;
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleMovement();
        HandleAttack();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
        controller.Move(movement);
    }

    private void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ShootProjectile();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PerformMeleeAttack();
        }
    }

    private void ShootProjectile()
    {
        Vector3 shootDirection = transform.forward;
        Vector3 spawnPosition = projectileSpawnPoint != null ?
            projectileSpawnPoint.transform.position : transform.position + Vector3.up;

        GameManager.Instance.SpawnProjectileServerRpc(
            spawnPosition,
            shootDirection,
            projectileSpeed,
            attack.Value,
            OwnerClientId
        );
    }

    private void PerformMeleeAttack()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * 2f, 1.5f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent<Player>(out Player otherPlayer) && otherPlayer != this)
            {
                otherPlayer.TakeDamageServerRpc(attack.Value, OwnerClientId);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, ulong attackerId)
    {
        health.Value -= damage;

        if (health.Value <= 0)
        {
            health.Value = 0;
            Respawn();
        }
    }

    private void Respawn()
    {
        GameManager.Instance.RespawnPlayerServerRpc(accoundID.Value.ToString());
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (IsOwner && newHealth <= 0)
        {
            print("Has muerto! Respawnando...");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag("Buff"))
        {
            CollectBuffServerRpc(other.GetComponent<NetworkObject>().NetworkObjectId);
        }
        if (other.CompareTag("Projectile"))
        {
            Projectile projectile = other.GetComponent<Projectile>();
            if (projectile != null && projectile.ownerId != OwnerClientId)
            {
                TakeDamageServerRpc(projectile.damage, projectile.ownerId);
                other.GetComponent<NetworkObject>().Despawn();
            }
        }
    }

    [ServerRpc]
    private void CollectBuffServerRpc(ulong buffNetId)
    {
        attack.Value += Random.Range(1, 4);
        print("Jugador " + accoundID.Value + " obtuvo un buff! Ataque: " + attack.Value);
        GameManager.Instance.BuffCollectedServerRpc(buffNetId);
    }
}
