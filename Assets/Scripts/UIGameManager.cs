using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIGameManager : NetworkBehaviour
{
    public TMP_InputField inputField;
    public Button submitButton;
    public GameObject LoginPanel;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI attackText;

    private Player localPlayer;

    void Start()
    {
        submitButton.onClick.AddListener(OnSubmitName);
        LoginPanel.SetActive(false);

        GameManager.Instance.OnConnection += () =>
        {
            LoginPanel.SetActive(true);
            inputField.text = "";
            submitButton.interactable = true;
            inputField.interactable = true;
        };
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null) return;

        if (localPlayer == null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>();
            if (localPlayer != null)
            {
                localPlayer.health.OnValueChanged += OnHealthChanged;
                localPlayer.attack.OnValueChanged += OnAttackChanged;
                UpdateUI();
            }
        }
    }


    public void OnSubmitName()
    {
        string accountID = inputField.text;
        if (!string.IsNullOrEmpty(accountID))
        {
            GameManager.Instance.RegisterPlayerServerRpc(accountID, NetworkManager.Singleton.LocalClientId);
            submitButton.interactable = false;
            inputField.interactable = false;
            LoginPanel.SetActive(false);
        }
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateUI();
    }

    private void OnAttackChanged(int oldAttack, int newAttack)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (localPlayer != null)
        {
            healthText.text = "Vida: " + localPlayer.health.Value;
            attackText.text = "Ataque: " + localPlayer.attack.Value;
        }
    }
}
