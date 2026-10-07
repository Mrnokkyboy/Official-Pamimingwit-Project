using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance { get; private set; }

    public int selectedIndex = 0;
    public int hotbarSize = 6;

    [Header("Audio")]
    public AudioClip switchSFX;
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        // Block keyboard/scroll selection whenever any UI panel is open
        if (UIManager.Instance != null && UIManager.Instance.IsUIOpen()) return;

        HandleSelectionInput();
        HandleUseInput();
    }

    private void HandleUseInput()
    {
        bool usePressed = false;

        if (InputHandler.Instance != null)
        {
            usePressed = InputHandler.Instance.WasActionPressed("Player/UseItem") || 
                         InputHandler.Instance.WasActionPressed("Player/Interact");
        }
        else if (Keyboard.current != null)
        {
            usePressed = Keyboard.current.eKey.wasPressedThisFrame;
        }

        if (usePressed)
        {
            Inventory.Instance?.TryUseSelectedHotbarConsumable();
        }
    }

    private void HandleSelectionInput()
    {
        if (hotbarSize <= 0) return;

        // 1. Mouse Scroll Wheel Selection
        float scroll = InputHandler.Instance != null ? InputHandler.Instance.GetHotbarScrollDelta() : 0f;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            int newIndex = selectedIndex - (int)Mathf.Sign(scroll);
            if (newIndex < 0) newIndex = hotbarSize - 1;
            if (newIndex >= hotbarSize) newIndex = 0;

            SelectSlot(newIndex);
            return;
        }

        // Input action names follow the asset ("Player/1" through "Player/5").
        // Fall back to direct keyboard input for slots without an asset binding.
        for (int i = 0; i < hotbarSize; i++)
        {
            string actionName = $"Player/{i + 1}";
            if (InputHandler.Instance != null && InputHandler.Instance.WasActionPressed(actionName))
            {
                SelectSlot(i);
                return;
            }

            if (Keyboard.current != null && Keyboard.current[Key.Digit1 + i].wasPressedThisFrame)
            {
                SelectSlot(i);
                return;
            }
        }
    }

    public void SelectSlot(int index)
    {
        if (index >= 0 && index < hotbarSize && index != selectedIndex)
        {
            selectedIndex = index;
            Inventory.Instance?.OnInventoryChanged?.Invoke();

            if (switchSFX != null && audioSource != null)
                audioSource.PlayOneShot(switchSFX);
        }
    }

    public ItemData GetSelectedItem()
    {
        if (Inventory.Instance == null || selectedIndex >= Inventory.Instance.itemList.Count)
            return null;

        return Inventory.Instance.itemList[selectedIndex].item;
    }
}