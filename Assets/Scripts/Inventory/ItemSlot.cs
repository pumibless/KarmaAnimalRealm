using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    [SerializeField]
    private Image m_icon;

    [SerializeField]
    private TextMesh m_label;

    [SerializeField]
    private GameObject m_stackNum;

    [SerializeField]
    private TextMesh m_stackLabel;

    public void Set(InventoryItem item) 
    {
        m_icon.sprite = item.data.icon;
        m_label.text = item.data.displayName;
        if (item.stackSize <= 1) 
        {
            m_stackNum.SetActive(false);
            return;
        }

        m_stackLabel.text = item.stackSize.ToString();
    }
}
