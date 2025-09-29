using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UILobby : MonoBehaviour
{
    public Button readyButton;
    public Button startButton;

    private PlayerLobby localPlayerLobby;

    void Start()
    {
        readyButton.onClick.AddListener(OnReadyClicked);
        startButton.onClick.AddListener(OnStartClicked);
        startButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (NetworkManager.Singleton.LocalClient != null && localPlayerLobby == null)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                localPlayerLobby = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerLobby>();

                if (NetworkManager.Singleton.IsHost)
                {
                    startButton.gameObject.SetActive(true);
                }
            }
        }
    }

    void OnReadyClicked()
    {
        if (localPlayerLobby != null)
        {
            localPlayerLobby.SetReady(!localPlayerLobby.isReady.Value);
        }
    }

    void OnStartClicked()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            LobbyManager.Instance.TryStartGame();
        }
    }
}
