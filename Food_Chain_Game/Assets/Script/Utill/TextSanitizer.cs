using System.Text;

// 플레이어가 입력한 글(닉네임, 채팅)을 서버에서 걸러낸다.
// 이 글들은 이름표, 말풍선, 플레이어 목록 등 여러 TextMeshPro 텍스트에 그대로 들어가므로,
// <size=300>, <color> 같은 태그가 적용되지 않도록 '<' 바로 뒤에 폭 없는 공백을 넣어 태그로 읽히지 않게 한다.
// (화면에는 입력한 글자가 그대로 보인다)
public static class TextSanitizer
{
    private const char ZeroWidthSpace = (char)0x200B;

    public static string BlockRichText(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0) return text;

        var sb = new StringBuilder(text.Length + 8);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            sb.Append(c);

            // 이미 처리된 글을 다시 넣어도 공백이 계속 늘어나지 않게 한다
            bool alreadyBlocked = i + 1 < text.Length && text[i + 1] == ZeroWidthSpace;
            if (c == '<' && !alreadyBlocked)
                sb.Append(ZeroWidthSpace);
        }
        return sb.ToString();
    }
}
