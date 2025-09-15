using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public GameObject playerPrefab;
    public GameObject projectilePrefab;
    public GameObject buffPrefab;
    public Dictionary<string, PlayerData> playerStatesByAccountID = new();

    public Vector2 mapBounds = new Vector2(20f, 20f);
    public int maxBuffs = 5;
    private int currentBuffs = 0;

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
            SpawnBuffs();
        }

        OnConnection?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
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
            Vector3 randomSpawn = GetRandomSpawnPosition();
            PlayerData NewData = new PlayerData(accountID, randomSpawn, 100, 5);
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

    public void SpawnPlayerServer(ulong ID, PlayerData data)
    {
        if (!IsServer) return;
        GameObject player = Instantiate(playerPrefab, data.position, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(ID, true);
        player.GetComponent<Player>().SetData(data);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnProjectileServerRpc(Vector3 position, Vector3 direction, float speed, int damage, ulong ownerId)
    {
        GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.identity);
        Projectile projScript = projectile.GetComponent<Projectile>();
        projScript.damage = damage;
        projScript.ownerId = ownerId;
        projScript.direction = direction;
        projScript.speed = speed;

        projectile.GetComponent<NetworkObject>().Spawn(true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RespawnPlayerServerRpc(string accountId)
    {
        if (playerStatesByAccountID.TryGetValue(accountId, out PlayerData data))
        {
            data.position = GetRandomSpawnPosition();
            data.health = 100;

            // Find the player object and update it
            foreach (var player in FindObjectsOfType<Player>())
            {
                if (player.accoundID.Value.ToString() == accountId)
                {
                    player.transform.position = data.position;
                    player.health.Value = data.health;
                    break;
                }
            }
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        return new Vector3(
            UnityEngine.Random.Range(-mapBounds.x, mapBounds.x),
            1f,
            UnityEngine.Random.Range(-mapBounds.y, mapBounds.y)
        );
    }

    private void SpawnBuffs()
    {
        if (!IsServer) return;

        for (int i = 0; i < maxBuffs; i++)
        {
            SpawnSingleBuff();
        }
    }

    private void SpawnSingleBuff()
    {
        Vector3 spawnPos = new Vector3(
            UnityEngine.Random.Range(-mapBounds.x * 0.8f, mapBounds.x * 0.8f),
            0.5f,
            UnityEngine.Random.Range(-mapBounds.y * 0.8f, mapBounds.y * 0.8f)
        );

        GameObject buff = Instantiate(buffPrefab, spawnPos, Quaternion.identity);
        buff.GetComponent<NetworkObject>().Spawn(true);
        currentBuffs++;
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuffCollectedServerRpc(ulong buffNetId)
    {
        NetworkObject buffObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[buffNetId];
        if (buffObject != null)
        {
            buffObject.Despawn();
            currentBuffs--;

      
            StartCoroutine(RespawnBuffAfterDelay());
        }
    }

    private System.Collections.IEnumerator RespawnBuffAfterDelay()
    {
        yield return new WaitForSeconds(10f);
        if (currentBuffs < maxBuffs)
        {
            SpawnSingleBuff();
        }
    }
}