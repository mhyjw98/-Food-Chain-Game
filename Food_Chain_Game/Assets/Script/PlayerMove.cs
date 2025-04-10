using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerMove : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rigid;

    public bool isStop = false;
    private TMP_InputField[] inputFields;
    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        inputFields = FindObjectsOfType<TMP_InputField>();
    }

    private void Update()
    {
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

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 input = new Vector2(h, v).normalized;

        Vector2 newPos = rigid.position + input * moveSpeed * Time.fixedDeltaTime;
        rigid.MovePosition(newPos);
       
    }
}
