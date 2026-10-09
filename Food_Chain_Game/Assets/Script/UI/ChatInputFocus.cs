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

    // 입력 중에 채팅창이 꺼지거나(게임 종료 화면 등) 씬이 바뀌면 Deselect 이벤트가 오지 않는다.
    // IsFocused는 static이라 그대로 두면 다음 씬에서도 "입력 중"으로 남아 이동이 막힌다
    private void OnDisable()
    {
        IsFocused = false;
    }
}
