using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TabGroup : MonoBehaviour
{
    //Declaring a list of tab buttons to use
    public List<TabButton> tabButtons;
    public Sprite tabIdleSprite;
    public Sprite tabHoverSprite;
    public Sprite tabSelectedSprite;

    //Method to subscribe a tab button to the group
    public void Subscribe(TabButton button)
    {
        if (tabButtons == null)
        {
            tabButtons = new List<TabButton>();
        }
        tabButtons.Add(button);
    }

    //3 methods to handle tabs
    public void OnTabEnter(TabButton button)
    {
        ResetTabs();
        button.background.sprite = tabHoverSprite;
    }

    public void OnTabExit(TabButton button)
    {
        ResetTabs();
    }

    public void OnTabSelected(TabButton button)
    {
        ResetTabs();
        button.background.sprite = tabSelectedSprite;
    }

    //Method to reset all tabs to default color
    public void ResetTabs()
    {
        foreach (TabButton button in tabButtons)
        {
            button.background.sprite = tabIdleSprite;
        }
    }
}
