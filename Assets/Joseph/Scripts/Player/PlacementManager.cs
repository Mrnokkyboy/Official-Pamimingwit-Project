using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance;

    [Header("Preview")]
    [SerializeField] private Color validColor = new Color(0.2f, 1f, 0.35f, 0.72f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.72f);
    [SerializeField] private string previewSortingLayer = "WalkInfront";
    [SerializeField] private int previewSortingOrder = 30;

    [Header("Placement Range")]
    [SerializeField] private Grid grid;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float obstacleCheckRadius = 0.25f;

    private GameObject previewObject;
    private SpriteRenderer previewRenderer;
    private DeployableData currentDeployable;
    private bool currentPlacementValid;
    private string currentInvalidReason;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (UIManager.Instance != null && (UIManager.Instance.IsUIOpen() || UIManager.Instance.IsPointerOverUI()))
        {
            CancelPlacement();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Normal)
        {
            CancelPlacement();
            return;
        }

        if (PlayerController.Instance != null && PlayerController.Instance.GetHeldItem() is DeployableData deployable)
        {
            currentDeployable = deployable;
            HandlePlacement();
        }
        else
        {
            CancelPlacement();
        }
    }

    private void HandlePlacement()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || Mouse.current == null || PlayerController.Instance == null)
        {
            CancelPlacement();
            return;
        }

        Vector3 screenPosition = Mouse.current.position.ReadValue();
        screenPosition.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0f;

        if (grid != null)
        {
            Vector3Int cellPosition = grid.WorldToCell(worldPosition);
            worldPosition = grid.GetCellCenterWorld(cellPosition);
            worldPosition.z = 0f;
        }

        float distance = Vector2.Distance(PlayerController.Instance.transform.position, worldPosition);
        bool inRange = distance >= currentDeployable.minDistance && distance <= currentDeployable.maxDistance;
        bool onWater = FishingManager.Instance != null &&
            Physics2D.OverlapCircle(worldPosition, obstacleCheckRadius, FishingManager.Instance.waterLayer) != null;
        bool isOccupied = Physics2D.OverlapCircle(worldPosition, obstacleCheckRadius, obstacleLayer) != null;

        currentPlacementValid = currentDeployable.worldPrefab != null &&
            inRange &&
            (!currentDeployable.requireWater || onWater) &&
            !isOccupied;
        currentInvalidReason = GetInvalidReason(inRange, onWater, isOccupied);

        EnsurePreview();
        previewObject.transform.position = worldPosition;
        previewRenderer.sprite = currentDeployable.icon;
        previewRenderer.color = currentPlacementValid ? validColor : invalidColor;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentPlacementValid)
            {
                PlaceDeployable(worldPosition);
            }
            else
            {
                UIManager.Instance?.ShowMessage(currentInvalidReason);
            }
        }
    }

    private string GetInvalidReason(bool inRange, bool onWater, bool isOccupied)
    {
        if (currentDeployable.worldPrefab == null) return "This deployable has no world prefab assigned.";
        if (!inRange) return $"Place this between {currentDeployable.minDistance:0.#} and {currentDeployable.maxDistance:0.#} units away.";
        if (currentDeployable.requireWater && !onWater) return "This deployable must be placed on water.";
        if (isOccupied) return "That spot is blocked. Try a nearby tile.";
        return "This spot cannot be used.";
    }

    private void EnsurePreview()
    {
        if (previewObject != null) return;

        previewObject = new GameObject("PlacementPreview");
        previewRenderer = previewObject.AddComponent<SpriteRenderer>();
        previewRenderer.sortingLayerName = previewSortingLayer;
        previewRenderer.sortingOrder = previewSortingOrder;
    }

    private void PlaceDeployable(Vector3 position)
    {
        if (Inventory.Instance == null || HotbarManager.Instance == null)
        {
            UIManager.Instance?.ShowMessage("Could not place this item because the inventory is unavailable.");
            return;
        }

        int index = HotbarManager.Instance.selectedIndex;
        if (index < 0 || index >= Inventory.Instance.itemList.Count)
        {
            UIManager.Instance?.ShowMessage("Could not place this item because its hotbar slot is invalid.");
            return;
        }

        InventoryItem slot = Inventory.Instance.itemList[index];
        if (slot.item != currentDeployable || slot.amount <= 0)
        {
            UIManager.Instance?.ShowMessage("The selected deployable is no longer in that hotbar slot.");
            return;
        }

        Instantiate(currentDeployable.worldPrefab, position, Quaternion.identity);
        slot.amount--;
        if (slot.amount <= 0)
        {
            slot.amount = 0;
            slot.item = null;
            slot.quality = FishQuality.None;
        }

        Inventory.Instance.OnInventoryChanged?.Invoke();
        UIManager.Instance?.ShowMessage($"Deployed {currentDeployable.itemName}!");
    }

    private void CancelPlacement()
    {
        if (previewObject != null) Destroy(previewObject);
        previewObject = null;
        previewRenderer = null;
        currentDeployable = null;
        currentPlacementValid = false;
    }
}
