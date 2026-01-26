using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class TabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    //We need a reference to the TabGroup this button belongs to
    public TabGroup tabGroup;

    //UI component handling the background image - whether its selected or not
    public Image background;

    void Start()
    {
        background = GetComponent<Image>();
        //Subscribe this button to the TabGroup
        tabGroup.Subscribe(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tabGroup.OnTabEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tabGroup.OnTabExit(this);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        tabGroup.OnTabSelected(this);
    }
}
