using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TabGroup : MonoBehaviour
{
    //Declaring a list of tab buttons to use
    public List<TabButton> tabButtons;
    public Sprite tabIdleSprite;
    public Sprite tabHoverSprite;
    public Sprite tabActiveSprite;
    public TabButton selectedTab; // Changed from Image to TabButton, renamed for clarity
    public List<GameObject> objectsToSwap;

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
        if(selectedTab == null || button != selectedTab) {
            button.background.sprite = tabHoverSprite;
        }
    }

    public void OnTabExit(TabButton button)
    {
        ResetTabs();
    }

    public void OnTabSelected(TabButton button)
    {
        selectedTab = button;
        ResetTabs();
        button.background.sprite = tabActiveSprite;
        int index = button.transform.GetSiblingIndex();
        for (int i = 0; i < objectsToSwap.Count; i++)
        {
            if (i == index)
            {
                objectsToSwap[i].SetActive(true);
            }
            else
            {
                objectsToSwap[i].SetActive(false);
            }
        }
    }

    //Method to reset all tabs to default color
    public void ResetTabs()
    {
        foreach (TabButton button in tabButtons)
        {
            if(selectedTab != null && button == selectedTab) {continue;}
            button.background.sprite = tabIdleSprite;
        }
    }
}
