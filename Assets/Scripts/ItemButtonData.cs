using UnityEngine;

public class ItemButtonData : MonoBehaviour
{
    public ItemData item;
    
    private void Awake()
    {
        Debug.Log($"[ItemButton Lifecycle] ItemButtonData Awake - GameObject: {gameObject.name}");
    }
    
    private void OnEnable()
    {
        Debug.Log($"[ItemButton Lifecycle] ItemButtonData OnEnable - GameObject: {gameObject.name}, Item: {(item != null ? item.name : "not set yet")}");
    }
    
    private void OnDisable()
    {
        Debug.Log($"[ItemButton Lifecycle] ItemButtonData OnDisable - GameObject: {gameObject.name}, Item: {(item != null ? item.name : "not set")}");
    }
    
    private void OnDestroy()
    {
        Debug.Log($"[ItemButton Lifecycle] ItemButtonData OnDestroy - GameObject: {gameObject.name}, Item: {(item != null ? item.name : "not set")}");
    }
} 