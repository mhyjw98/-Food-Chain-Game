using UnityEngine;

// 의상 하나(강아지, 고양이, 토끼 등). 그림을 넣기만 하면 로비 미리보기와 게임 캐릭터에 바로 적용된다.
// 비어 있는 그림은 기본 모습을 그대로 쓴다
[CreateAssetMenu(menuName = "Costume/Costume Data", fileName = "Costume")]
public class CostumeData : ScriptableObject
{
    [Tooltip("커스터마이징 창에 보일 이름")]
    public string displayName;

    [Tooltip("선택 버튼에 보일 아이콘. 비어 있으면 몸 그림, 그것도 없으면 이름만 보인다")]
    public Sprite icon;

    [Tooltip("캐릭터 몸 그림. 플레이어가 고른 색이 입혀진다(흰색 바탕으로 그리면 색이 그대로 나온다). 비어 있으면 기본 몸 그림")]
    public Sprite bodySprite;

    [Tooltip("몸 위에 덮는 그림(눈, 코, 무늬 등 색이 바뀌면 안 되는 부분). 비어 있으면 없음")]
    public Sprite overlaySprite;

    [Tooltip("켜면 덮는 그림에도 플레이어 색을 입힌다")]
    public bool tintOverlay;

    public Sprite ButtonIcon => icon != null ? icon : bodySprite;
}
