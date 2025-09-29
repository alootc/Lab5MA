using Unity.Netcode;
using UnityEngine;

public class PlayerLobby : NetworkBehaviour
{
    public NetworkVariable<string> playerName = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isReady = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void SetReady(bool ready)
    {
        SetReadyServerRpc(ready);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetReadyServerRpc(bool ready, ServerRpcParams rpcParams = default)
    {
        isReady.Value = ready;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        playerName.Value = name;
    }
}
