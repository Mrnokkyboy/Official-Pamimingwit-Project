using System.Collections.Generic;
using UnityEngine;

public class ItemDictionary : MonoBehaviour
{
    public static ItemDictionary Instance { get; private set; }

    [Header("Item Database")]
    [Tooltip("List of all ItemData ScriptableObjects in the game.")]
    [SerializeField] private List<ItemData> itemDatabase = new List<ItemData>();

    private readonly Dictionary<int, GameObject> prefabDictionary = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, ItemData> dataDictionary = new Dictionary<int, ItemData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        prefabDictionary.Clear();
        dataDictionary.Clear();

        foreach (ItemData item in itemDatabase)
        {
            if (item == null)
            {
                Debug.LogWarning("[ItemDictionary] Null ItemData entry found in Inspector list.");
                continue;
            }

            if (dataDictionary.ContainsKey(item.ID))
            {
                Debug.LogError($"[ItemDictionary] Duplicate Item ID '{item.ID}' detected on '{item.itemName}'! Skipping registration.");
                continue;
            }

            dataDictionary[item.ID] = item;

            if (item.prefab != null)
            {
                prefabDictionary[item.ID] = item.prefab;
            }
            else
            {
                Debug.LogWarning($"[ItemDictionary] Item '{item.itemName}' (ID: {item.ID}) has no prefab assigned.");
            }
        }
    }

    /// <summary>
    /// Retrieves the GameObject prefab associated with a given Item ID.
    /// </summary>
    public GameObject GetItemPrefab(int itemID)
    {
        if (prefabDictionary.TryGetValue(itemID, out GameObject prefab))
        {
            return prefab;
        }

        Debug.LogWarning($"[ItemDictionary] Prefab for Item ID '{itemID}' not found or unassigned.");
        return null;
    }

    /// <summary>
    /// Retrieves the ItemData ScriptableObject associated with a given Item ID.
    /// </summary>
    public ItemData GetItemData(int itemID)
    {
        if (dataDictionary.TryGetValue(itemID, out ItemData data))
        {
            return data;
        }

        Debug.LogWarning($"[ItemDictionary] ItemData for Item ID '{itemID}' not found.");
        return null;
    }
}