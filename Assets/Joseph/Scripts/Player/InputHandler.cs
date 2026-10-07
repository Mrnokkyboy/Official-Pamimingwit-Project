using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }

    private static readonly List<Interactable> registeredInteractables = new List<Interactable>();

    [Header("Interaction Settings")]
    [SerializeField] private float defaultInteractionRadius = 2.0f;

    private PlayerInput playerInput;

    public Vector2 MoveInput { get; private set; }
    public bool ClickDown { get; private set; }
    public bool ClickHeld { get; private set; }
    public bool ClickUp { get; private set; }
    public bool RightClickDown { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool InventoryPressed { get; private set; }
    public bool CraftingPressed { get; private set; }
    public bool CancelPressed { get; private set; }
    public bool RotatePressed { get; private set; }

    public static InputHandler EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        InputHandler found = FindObjectOfType<InputHandler>(true);
        if (found != null)
        {
            Instance = found;
            return found;
        }

        GameObject go = new GameObject("InputHandler");
        Instance = go.AddComponent<InputHandler>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerInput = GetComponent<PlayerInput>() ?? FindFirstObjectByType<PlayerInput>();
    }

    private void OnEnable()
    {
        BindInputActions();
    }

    private void OnDisable()
    {
        UnbindInputActions();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            TriggerInteract();

        if (Keyboard.current.tabKey.wasPressedThisFrame)
            TriggerInventory();

        if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame)
            TriggerCrafting();

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            TriggerCancel();

        if (Keyboard.current.rKey.wasPressedThisFrame)
            RotatePressed = true;
    }

    private void LateUpdate()
    {
        ClickDown = false;
        ClickUp = false;
        RightClickDown = false;

        InteractPressed = false;
        InventoryPressed = false;
        CraftingPressed = false;
        CancelPressed = false;
        RotatePressed = false;
    }

    private void BindInputActions()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>() ?? FindFirstObjectByType<PlayerInput>();

        if (playerInput == null || playerInput.actions == null)
            return;

        playerInput.actions.Enable();

        BindAction("Move", OnMove);
        BindAction("Interact", OnInteract);
        BindAction("Inventory", OnInventory);
        BindAction("Crafting", OnCrafting);
        BindAction("Cancel", OnCancel);
        BindAction("Rotate", OnRotate);
        BindAction("1", OnSlot1);
        BindAction("2", OnSlot2);
        BindAction("3", OnSlot3);
        BindAction("4", OnSlot4);
        BindAction("5", OnSlot5);
    }

    private void UnbindInputActions()
    {
        if (playerInput == null || playerInput.actions == null)
            return;

        UnbindAction("Move", OnMove);
        UnbindAction("Interact", OnInteract);
        UnbindAction("Inventory", OnInventory);
        UnbindAction("Crafting", OnCrafting);
        UnbindAction("Cancel", OnCancel);
        UnbindAction("Rotate", OnRotate);
        UnbindAction("1", OnSlot1);
        UnbindAction("2", OnSlot2);
        UnbindAction("3", OnSlot3);
        UnbindAction("4", OnSlot4);
        UnbindAction("5", OnSlot5);
    }

    private void BindAction(string actionName, System.Action<InputAction.CallbackContext> callback)
    {
        if (playerInput == null || playerInput.actions == null)
            return;

        InputAction action = playerInput.actions.FindAction(actionName, false);
        if (action == null)
            return;

        action.started -= callback;
        action.performed -= callback;
        action.canceled -= callback;

        action.started += callback;
        action.performed += callback;
        action.canceled += callback;
    }

    private void UnbindAction(string actionName, System.Action<InputAction.CallbackContext> callback)
    {
        if (playerInput == null || playerInput.actions == null)
            return;

        InputAction action = playerInput.actions.FindAction(actionName, false);
        if (action == null)
            return;

        action.started -= callback;
        action.performed -= callback;
        action.canceled -= callback;
    }

    public static void RegisterInteractable(Interactable interactable)
    {
        if (interactable != null && !registeredInteractables.Contains(interactable))
            registeredInteractables.Add(interactable);
    }

    public static void UnregisterInteractable(Interactable interactable)
    {
        if (interactable != null && registeredInteractables.Contains(interactable))
            registeredInteractables.Remove(interactable);
    }

    public InputAction FindAction(string actionPath)
    {
        if (playerInput != null && playerInput.actions != null)
        {
            InputAction action = playerInput.actions.FindAction(actionPath, false);
            if (action != null)
                return action;
        }

        if (InputSystem.actions != null)
            return InputSystem.actions.FindAction(actionPath, false);

        return null;
    }

    public float GetHotbarScrollDelta()
    {
        float mouseScroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
        if (Mathf.Abs(mouseScroll) > 0.01f)
            return mouseScroll;

        if (Gamepad.current != null)
        {
            if (Gamepad.current.rightShoulder.wasPressedThisFrame)
                return -1f;
            if (Gamepad.current.leftShoulder.wasPressedThisFrame)
                return 1f;
        }

        return 0f;
    }

    public bool WasActionPressed(string actionPath)
    {
        InputAction action = FindAction(actionPath);
        return action != null && action.WasPressedThisFrame();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            ClickDown = true;
            ClickHeld = true;
        }

        if (context.canceled)
        {
            ClickUp = true;
            ClickHeld = false;
        }
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        if (context.started)
            RightClickDown = true;
    }

    public void OnSlot1(InputAction.CallbackContext context) { if (context.started || context.performed) SelectSlot(0); }
    public void OnSlot2(InputAction.CallbackContext context) { if (context.started || context.performed) SelectSlot(1); }
    public void OnSlot3(InputAction.CallbackContext context) { if (context.started || context.performed) SelectSlot(2); }
    public void OnSlot4(InputAction.CallbackContext context) { if (context.started || context.performed) SelectSlot(3); }
    public void OnSlot5(InputAction.CallbackContext context) { if (context.started || context.performed) SelectSlot(4); }

    private void SelectSlot(int slotIndex)
    {
        if (HotbarManager.Instance == null)
            return;

        if (slotIndex >= 0 && slotIndex < HotbarManager.Instance.hotbarSize)
            HotbarManager.Instance.SelectSlot(slotIndex);
    }

    private void TriggerInteract()
    {
        InteractPressed = true;

        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.UI)
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                DialogueManager.Instance.AdvanceDialogue();
                return;
            }

            if (CutsceneManager.Instance != null && CutsceneManager.Instance.IsCutsceneActive)
            {
                CutsceneManager.Instance.AdvanceCutscene();
                return;
            }
        }

        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Normal)
            return;

        Interactable closest = FindClosest();
        if (closest != null)
            closest.Interact();
    }

    private void TriggerInventory()
    {
        InventoryPressed = true;

        if (PlayerUIManager.Instance != null)
            PlayerUIManager.Instance.ToggleInventory();
    }

    private void TriggerCrafting()
    {
        CraftingPressed = true;

        if (UIManager.Instance != null)
            UIManager.Instance.ToggleCrafting();
    }

    private void TriggerCancel()
    {
        CancelPressed = true;
        UIManager.Instance?.CloseAllStandardPanels();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
            TriggerInteract();
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
            TriggerInventory();
    }

    public void OnCrafting(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
            TriggerCrafting();
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
            TriggerCancel();
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
            RotatePressed = true;
    }

    private Interactable FindClosest()
    {
        Vector3 playerPos = PlayerController.Instance != null
            ? PlayerController.Instance.transform.position
            : transform.position;

        float minDistanceSq = defaultInteractionRadius * defaultInteractionRadius;
        Interactable closest = null;

        for (int i = registeredInteractables.Count - 1; i >= 0; i--)
        {
            Interactable candidate = registeredInteractables[i];

            if (candidate == null)
            {
                registeredInteractables.RemoveAt(i);
                continue;
            }

            float sqrDist = (playerPos - candidate.transform.position).sqrMagnitude;
            if (sqrDist < minDistanceSq)
            {
                minDistanceSq = sqrDist;
                closest = candidate;
            }
        }

        return closest;
    }
}
