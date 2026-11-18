using UnityEngine;
using UnityEngine.EventSystems;

// 버튼에 붙이면, 마우스를 올렸을 때 GameOverMenu 의 화살표가 이 버튼 옆으로 이동합니다.
public class MenuArrowTarget : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    public GameOverMenu menu;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (menu != null) menu.MoveArrowTo((RectTransform)transform);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (menu != null) menu.MoveArrowTo((RectTransform)transform);
    }
}
