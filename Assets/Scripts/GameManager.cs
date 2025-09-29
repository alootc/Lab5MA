//using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Player;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public GameObject playerPrefab;
    public GameObject projectilePrefab;
    public GameObject buffPrefab;
    public Dictionary<string, PlayerData> playerStatesByAccountID = new();

    public Vector2 respawnAreaMin = new Vector2(-20, -20);
    public Vector2 respawnAreaMax = new Vector2(20, 20);

    public Action OnConnection;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
            SpawnInitialBuffs();
        }

        OnConnection?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
    }

    private void SpawnInitialBuffs()
    {
        if (!IsServer) return;

        if (buffPrefab == null)
        {
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            SpawnBuff();
        }
    }

    private void HandleDisconnect(ulong clientID)
    {
        print("El jugador " + clientID + " se ha desconectado");
    }

    [Rpc(SendTo.Server)]
    public void RegisterPlayerServerRpc(string accountID, ulong ID)
    {
        if (!playerStatesByAccountID.TryGetValue(accountID, out PlayerData data))
        {
            Vector3 randomPosition = GetRandomSpawnPosition();
            PlayerData NewData = new PlayerData(accountID, randomPosition, 100, 5);
            playerStatesByAccountID[accountID] = NewData;
            SpawnPlayerServer(ID, NewData);
            print("Nueva id creada con el nombre de " + accountID);
        }
        else
        {
            print("Se encontró cuenta con el nombre de " + accountID);
            SpawnPlayerServer(ID, data);
        }
    }

    public Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(respawnAreaMin.x, respawnAreaMax.x);
        float z = Random.Range(respawnAreaMin.y, respawnAreaMax.y);
        return new Vector3(x, 0, z);
    }

    public void SpawnPlayerServer(ulong ID, PlayerData data)
    {
        if (!IsServer) return;

        if (playerPrefab == null)
        {
            return;
        }

        GameObject player = Instantiate(playerPrefab, data.position, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(ID, true);
        player.GetComponent<Player>().SetData(data);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnProjectileServerRpc(Vector3 position, Vector3 direction, float speed, int damage, ulong ownerId)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("projectilePrefab no está asignado en el Inspector. No se puede generar el proyectil.");
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.identity);
        Projectile projComponent = projectile.GetComponent<Projectile>();
        projComponent.damage = damage;
        projComponent.ownerId = ownerId;

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        projectile.GetComponent<NetworkObject>().Spawn(true);

        Destroy(projectile, 5f);
    }

    [ServerRpc]
    public void RespawnPlayerServerRpc(string accountId)
    {
        if (playerStatesByAccountID.TryGetValue(accountId, out PlayerData data))
        {
            data.health = 100;
            data.position = GetRandomSpawnPosition();

            Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (var playerObj in players)
            {
                if (playerObj.accoundID.Value.ToString() == accountId)
                {
                    playerObj.transform.position = data.position;
                    playerObj.health.Value = data.health;
                    break;
                }
            }
        }
    }

    [ServerRpc]
    public void BuffCollectedServerRpc(ulong buffNetId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(buffNetId, out NetworkObject buffObject))
        {
            if (buffObject != null)
            {
                buffObject.Despawn();
                StartCoroutine(RespawnBuffAfterDelay(5f));
            }
        }
    }

    private IEnumerator RespawnBuffAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnBuff();
    }

    private void SpawnBuff()
    {
        if (!IsServer || buffPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = new Vector3(
            Random.Range(-45f, 45f),
            0.5f,
            Random.Range(-45f, 45f)
        );

        GameObject buff = Instantiate(buffPrefab, spawnPosition, Quaternion.identity);
        buff.GetComponent<NetworkObject>().Spawn(true);
    }
}
