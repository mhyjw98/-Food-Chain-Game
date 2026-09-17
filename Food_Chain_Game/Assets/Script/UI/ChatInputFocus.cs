using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatInputFocus : MonoBehaviour
{
    public static bool IsFocused { get; private set; }

    public void OnInputSelected()
    {
        IsFocused = true;
    }

    public void OnInputDeselected()
    {
        IsFocused = false;
    }
}
