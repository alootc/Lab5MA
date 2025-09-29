using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void TryStartGame()
    {
        if (!IsServer) return; 

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var lobby = client.PlayerObject.GetComponent<PlayerLobby>();
            if (lobby == null || !lobby.isReady.Value)
            {
                Debug.Log("Un jugador no está listo.");
                return;
            }
        }

        Debug.Log("Todos listos. Iniciando partida...");
        NetworkManager.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
