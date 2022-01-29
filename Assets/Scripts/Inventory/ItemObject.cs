using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemObject : MonoBehaviour
{
    [Header("Inventory")]
    public InventoryItemData referenceItem;

    public void OnHandlePickupItem() 
    {
        InventorySystem.current.Add(referenceItem);

        //make instance disappear preferrably by animation
        Destroy(gameObject);
        
    }
}
