using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    // Since your scene has an "ItemsMenu" GameObject with a GridLayoutGroup
    [SerializeField] private GameObject itemsMenu; // Reference to the ItemsMenu object
    [SerializeField] private GameObject itemButtonPrefab; // Prefab for item buttons (MenuButton or similar)
    
    private PlayerInventory playerInventory;
    private List<GameObject> itemButtons = new List<GameObject>();
    
    private void Awake()
    {
        // Find the ItemsMenu if not set
        if (itemsMenu == null)
        {
            // Looking specifically for the ItemsMenu in your scene hierarchy
            Transform menuPanel = transform.Find("MenuPanel");
            if (menuPanel != null)
            {
                itemsMenu = menuPanel.Find("ItemsMenu")?.gameObject;
                if (itemsMenu != null)
                {
                    Debug.Log("Found ItemsMenu in MenuPanel");
                }
            }
        }
        
        // Find or set the button prefab using existing prefabs
        if (itemButtonPrefab == null)
        {
            // Use a MenuButton prefab that exists in your project
            itemButtonPrefab = Resources.Load<GameObject>("MenuButton") ?? 
                               Resources.Load<GameObject>("prefabs/MenuButton");
            
            if (itemButtonPrefab != null)
            {
                Debug.Log("Found MenuButton prefab");
            }
        }
    }
    
    public void SetInventory(PlayerInventory inventory)
    {
        // Unsubscribe from old inventory if exists
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= RefreshInventoryUI;
        }
        
        if (inventory == null)
        {
            Debug.LogError("Trying to set null inventory to UI!");
            return;
        }
        
        playerInventory = inventory;
        playerInventory.OnInventoryChanged += RefreshInventoryUI;
        
        // Initial refresh
        RefreshInventoryUI();
    }
    
    public void RefreshInventoryUI()
    {
        // Clear existing buttons
        ClearItemButtons();
        
        if (playerInventory == null)
        {
            Debug.LogError("No player inventory assigned to UI!");
            return;
        }
        
        if (itemsMenu == null)
        {
            Debug.LogError("ItemsMenu not found in UI!");
            return;
        }
        
        if (itemButtonPrefab == null)
        {
            Debug.LogError("No item button prefab assigned or found!");
            return;
        }
        
        // Get or find the existing InventoryItem in the ItemsMenu
        Transform inventoryItem = itemsMenu.transform.Find("InventoryItem");
        bool hasExistingTemplate = inventoryItem != null;
        
        // If the example item exists, hide it so we can use it as a template
        if (hasExistingTemplate)
        {
            inventoryItem.gameObject.SetActive(false);
        }
        
        // Create a button for each item in inventory
        foreach (ItemData item in playerInventory.Items)
        {
            GameObject buttonObj;
            
            if (hasExistingTemplate)
            {
                // Clone the existing item template
                buttonObj = Instantiate(inventoryItem.gameObject, itemsMenu.transform);
                buttonObj.SetActive(true);
            }
            else
            {
                // Use the prefab
                buttonObj = Instantiate(itemButtonPrefab, itemsMenu.transform);
            }
            
            // Look for a TextMeshProUGUI component in children (your UI might have "Name" text)
            TextMeshProUGUI nameText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = $"{item.name} x{item.amount}";
            }
            
            // Add button functionality
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObj.AddComponent<Button>();
            }
            
            // Add item data reference to the button
            ItemButtonData buttonData = buttonObj.GetComponent<ItemButtonData>();
            if (buttonData == null)
            {
                buttonData = buttonObj.AddComponent<ItemButtonData>();
            }
            buttonData.item = item;
            
            // Add click handler
            ItemData capturedItem = item; // Capture the item in a local variable
            button.onClick.AddListener(() => OnItemButtonClicked(capturedItem));
            
            // Add to our list for cleanup
            itemButtons.Add(buttonObj);
        }
        
        // If no items and no template exists, show empty message
        if (playerInventory.Items.Count == 0 && !hasExistingTemplate)
        {
            GameObject emptyText = new GameObject("EmptyText");
            emptyText.transform.SetParent(itemsMenu.transform, false);
            
            TextMeshProUGUI textComp = emptyText.AddComponent<TextMeshProUGUI>();
            textComp.text = "No items";
            textComp.fontSize = 24;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.color = Color.white;
            
            // Add to list for cleanup
            itemButtons.Add(emptyText);
        }
    }
    
    private void OnItemButtonClicked(ItemData item)
    {
        Debug.Log($"Item clicked: {item.name}");
        // Implement item use logic here
    }
    
    private void ClearItemButtons()
    {
        foreach (GameObject button in itemButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        
        itemButtons.Clear();
    }
    
    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= RefreshInventoryUI;
        }
    }
} 