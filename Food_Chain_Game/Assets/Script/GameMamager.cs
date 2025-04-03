using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMamager : MonoBehaviour
{
    public static GameMamager Instance;
    public GameObject characterPanel;
    public Text characterText;

    private void Awake()
    {
        Instance = this;
        
    }
    private void Start()
    {
        RoomManager.Instance.ServerAssignCharacters();
    }
    public void ShowCharacter(string name)
    {
        Debug.Log($"[GameManager] ShowCharacter 호출됨: {name}");
        characterPanel.SetActive(true);
        characterText.text = $"내 캐릭터: {name}";
    }
}
