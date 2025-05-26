using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMove : NetworkBehaviour
{
    public Scanner scanner;

    [SyncVar]public float moveSpeed = 5f;
    public Vector2 lastMoveDirection = new(1, 0);
    private Rigidbody2D rigid;

    public bool isStop = false;
    public static bool isEvent = false;
    private static TMP_InputField[] inputFields;
    private GamePlayer localPlayer;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        inputFields = FindObjectsOfType<TMP_InputField>();
        localPlayer = GetComponent<GamePlayer>();
    }

    private void Update()
    {
        if (scanner != null && isLocalPlayer && GameMamager.Instance && TryGetComponent(out GamePlayer gp))
            scanner.UpdateKillUI(localPlayer);

        isStop = false;

        foreach (var input in inputFields)
        {
            if (input.isFocused)
            {
                isStop = true;
                break;
            }
        }
    }
    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        if (isStop) return;
        if (isEvent) return;
        if (GameMamager.Instance != null)
        {
            if(!GameMamager.Instance.isRoundActive)
                return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 input = new Vector2(h, v).normalized;

        if (input.x != 0)
        {
            lastMoveDirection = input;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (TryGetComponent(out GamePlayer gp))
            {
                gp.CmdAttack();
            }
        }
        Vector2 newPos = rigid.position + input * moveSpeed * Time.fixedDeltaTime;
        rigid.MovePosition(newPos);
    }
    public static void RegisterInputField()
    {
        inputFields = FindObjectsOfType<TMP_InputField>();
    }
}
