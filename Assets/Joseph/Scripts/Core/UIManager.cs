using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public enum ActiveShopType { None, General, Industrial, TrashDisposal }

    [Header("Shop State")]
    public ActiveShopType currentShopType = ActiveShopType.None;

    [Header("Shop UI")]
    public GameObject shopPanel;
    public Transform shopContent;
    public GameObject industrialShopPanel;
    public Transform industrialShopContent;
    public GameObject shopSlotPrefab;
    public GameObject trashDisposalPanel;

    [Header("Day Cycle UI")]
    public TextMeshProUGUI dayHUDText;
    [SerializeField] private Image weatherIcon;
    [SerializeField] private Sprite sunnyWeatherIcon;
    [SerializeField] private Sprite cloudyWeatherIcon;
    [SerializeField] private Sprite rainingWeatherIcon;
    public CanvasGroup dayTransitionOverlay;
    public TextMeshProUGUI dayTransitionText;
    public TextMeshProUGUI taxTransitionText;
    public TextMeshProUGUI fatigueNoticeText; // Text display for passing out

    [SerializeField] private float typewriterSpeed = 0.05f;
    [SerializeField] private float overlayFadeDuration = 2.0f;
    [SerializeField] private float taxFadeDuration = 1.0f;
    [SerializeField] private float stayBlackDuration = 2.0f;

    [Header("Cleaning Mini-game UI")]
    public GameObject choicePanel;
    public TextMeshProUGUI choicePromptText;
    public Button throwInOceanButton;
    public TextMeshProUGUI throwInOceanText;
    public Button cleanUpButton;
    public TextMeshProUGUI cleanUpText;
    public GameObject cleaningPanel;
    public GameObject seaweedPanel;

    [Header("Cutscene UI")]
    public GameObject cutscenePanel;
    public CanvasGroup cutsceneFadeOverlay;
    public Image cutsceneBackground;
    public Image cutsceneCharacter;
    public TextMeshProUGUI cutsceneNameText;
    public TextMeshProUGUI cutsceneContentText;

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueNameText;
    public TextMeshProUGUI dialogueContentText;
    public Image dialogueIcon;

    [Header("Crafting UI")]
    public GameObject craftingPanel;
    public CraftingResultSlot resultSlot;

    [Header("Quest UI")]
    [SerializeField] private GameObject questPanel;

    [Header("Fish Index UI")]
    public GameObject fishIndexPanel;
    public Transform fishIndexGrid;
    public GameObject fishIndexEntryPrefab;
    public Button fishIndexToggleButton;
    public Sprite fishIndexLockedSprite;

    [Header("Game Messages")]
    [SerializeField] private TextMeshProUGUI messageText;

    private Coroutine activeDayTransitionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        HideCraftingPanel();
        HideShopPanel();
        if (choicePanel != null) choicePanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
        if (cleaningPanel != null) cleaningPanel.SetActive(false);
        if (seaweedPanel != null) seaweedPanel.SetActive(false);
        if (cutscenePanel != null) cutscenePanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (fishIndexPanel != null) fishIndexPanel.SetActive(false);

        GameEvents.OnItemCaught += RefreshFishIndex;

        if (fishIndexToggleButton != null)
        {
            fishIndexToggleButton.onClick.RemoveAllListeners();
            fishIndexToggleButton.onClick.AddListener(ToggleFishIndex);
        }

        RefreshFishIndex();

        if (WeatherManager.Instance != null)
        {
            UpdateWeatherIcon(WeatherManager.Instance.CurrentWeather);
        }

        if (cutsceneFadeOverlay != null && (CutsceneManager.Instance == null || !CutsceneManager.Instance.IsCutsceneActive))
        {
            cutsceneFadeOverlay.alpha = 0f;
        }

        if (dayTransitionOverlay != null)
        {
            dayTransitionOverlay.alpha = SaveController.shouldLoadGame ? 1f : 0f;
        }
    }

    private void OnDestroy()
    {
        GameEvents.OnItemCaught -= RefreshFishIndex;
    }

    private void OnEnable()
    {
        WeatherManager.OnWeatherChanged += UpdateWeatherIcon;
    }

    private void OnDisable()
    {
        WeatherManager.OnWeatherChanged -= UpdateWeatherIcon;
    }

    private void UpdateWeatherIcon(WeatherManager.WeatherState weather)
    {
        if (weatherIcon == null)
            return;

        switch (weather)
        {
            case WeatherManager.WeatherState.Sunny:
                weatherIcon.sprite = sunnyWeatherIcon;
                break;
            case WeatherManager.WeatherState.Cloudy:
                weatherIcon.sprite = cloudyWeatherIcon;
                break;
            case WeatherManager.WeatherState.Raining:
                weatherIcon.sprite = rainingWeatherIcon;
                break;
        }

        weatherIcon.enabled = weatherIcon.sprite != null;
    }

    // ---------------------------
    // DAY TRANSITION SYSTEM
    // ---------------------------
    public void StartDayTransition(Action onFadeComplete, bool passedOut = false)
    {
        if (activeDayTransitionCoroutine != null)
        {
            StopCoroutine(activeDayTransitionCoroutine);
        }
        activeDayTransitionCoroutine = StartCoroutine(DayTransitionRoutine(onFadeComplete, passedOut));
    }

    private IEnumerator DayTransitionRoutine(Action onFadeComplete, bool passedOut)
    {
        if (dayTransitionOverlay == null) yield break;

        GameManager.Instance?.SetState(GameState.UI);
        PlayerController.Instance?.LockMovement();

        if (dayTransitionText != null) { dayTransitionText.text = ""; dayTransitionText.alpha = 1f; }
        if (taxTransitionText != null) { taxTransitionText.text = ""; taxTransitionText.alpha = 0f; }
        if (fatigueNoticeText != null) { fatigueNoticeText.text = ""; fatigueNoticeText.alpha = 0f; }

        // Fade Screen to Black
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / overlayFadeDuration;
            dayTransitionOverlay.alpha = Mathf.Min(t, 1f);
            yield return null;
        }
        dayTransitionOverlay.alpha = 1f;

        // Display Fatigue Text if the player passed out
        if (passedOut && fatigueNoticeText != null)
        {
            fatigueNoticeText.alpha = 1f;
            yield return StartCoroutine(TypewriterEffect(fatigueNoticeText, "You passed out from exhaustion..."));
            yield return new WaitForSeconds(1.2f);

            // Fade out fatigue notice before showing Day number
            float noticeFade = 1f;
            while (noticeFade > 0f)
            {
                noticeFade -= Time.deltaTime / taxFadeDuration;
                fatigueNoticeText.alpha = noticeFade;
                yield return null;
            }
            fatigueNoticeText.alpha = 0f;
        }

        GameManager.Instance?.AdvanceDay();
        GameEvents.TriggerSound(SoundType.Rooster);

        if (dayTransitionText != null && GameManager.Instance != null)
        {
            string dayName = GameManager.Instance.GetCurrentDayOfWeekName();
            string dayStr = $"DAY: {dayName} ({GameManager.Instance.currentDay})";
            yield return StartCoroutine(TypewriterEffect(dayTransitionText, dayStr));
        }

        if (taxTransitionText != null && GameManager.Instance != null)
        {
            string taxMessage = GameManager.Instance.ProcessTax();
            taxTransitionText.text = taxMessage;

            if (taxMessage.Contains("deducted"))
                GameEvents.TriggerSound(SoundType.TaxPaid);
            else if (taxMessage.Contains("doubles"))
                GameEvents.TriggerSound(SoundType.TaxFailed);

            float textFadeT = 0f;
            while (textFadeT < 1f)
            {
                textFadeT += Time.deltaTime / taxFadeDuration;
                taxTransitionText.alpha = textFadeT;
                yield return null;
            }
            taxTransitionText.alpha = 1f;
        }

        onFadeComplete?.Invoke();

        GameEvents.TriggerSound(SoundType.MorningAmbience);
        yield return new WaitForSeconds(stayBlackDuration);

        float textFadeOutT = 1f;
        while (textFadeOutT > 0f)
        {
            textFadeOutT -= Time.deltaTime / taxFadeDuration;
            if (dayTransitionText != null) dayTransitionText.alpha = textFadeOutT;
            if (taxTransitionText != null) taxTransitionText.alpha = textFadeOutT;
            yield return null;
        }

        if (dayTransitionText != null) dayTransitionText.alpha = 0f;
        if (taxTransitionText != null) taxTransitionText.alpha = 0f;

        float fadeOutT = 1f;
        while (fadeOutT > 0f)
        {
            fadeOutT -= Time.deltaTime / overlayFadeDuration;
            dayTransitionOverlay.alpha = Mathf.Max(fadeOutT, 0f);
            yield return null;
        }

        GameEvents.TriggerSound(SoundType.StopTransitionAudio);
        dayTransitionOverlay.alpha = 0f;

        GameManager.Instance?.SetState(GameState.Normal);
        PlayerController.Instance?.UnlockMovement();
        activeDayTransitionCoroutine = null;
    }

    private IEnumerator TypewriterEffect(TextMeshProUGUI textComponent, string content)
    {
        textComponent.text = "";
        foreach (char c in content.ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    // ---------------------------
    // CENTRALIZED PANEL MANAGEMENT
    // ---------------------------
    public void TogglePanelState(GameObject panel, bool? forceState = null, bool playSFX = true)
    {
        if (panel == null) return;

        bool targetState = forceState ?? !panel.activeSelf;
        if (panel.activeSelf == targetState) return;

        panel.SetActive(targetState);

        if (targetState)
        {
            if (playSFX) PlayPanelOpenSFX();
            GameManager.Instance?.SetState(GameState.UI);
            PlayerController.Instance?.LockMovement();
        }
        else
        {
            if (playSFX) PlayPanelCloseSFX();
            CheckAndRestoreState();
        }
    }

    // ---------------------------
    // SHOPS
    // ---------------------------
    private void RefreshShopStock(System.Collections.Generic.List<ShopStockEntry> stock, Transform targetContent)
    {
        if (targetContent == null || shopSlotPrefab == null) return;

        foreach (Transform child in targetContent)
        {
            Destroy(child.gameObject);
        }

        foreach (ShopStockEntry entry in stock)
        {
            GameObject obj = Instantiate(shopSlotPrefab, targetContent);
            ShopSlotUI slot = obj.GetComponent<ShopSlotUI>();
            if (slot != null) slot.Setup(entry.item, entry.GetPrice(), entry.currentStock);
        }
    }

    public void OpenShop()
    {
        if (shopPanel == null) return;
        currentShopType = ActiveShopType.General;
        if (ShopManager.Instance != null) RefreshShopStock(ShopManager.Instance.GetAvailableStock(), shopContent);
        TogglePanelState(shopPanel, true);

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.InventoryPanel != null)
        {
            TogglePanelState(PlayerUIManager.Instance.InventoryPanel, true, false);
        }
    }

    public void OpenIndustrialShop()
    {
        if (industrialShopPanel == null) return;
        currentShopType = ActiveShopType.Industrial;
        if (IndustrialShopManager.Instance != null) RefreshShopStock(IndustrialShopManager.Instance.GetAvailableStock(), industrialShopContent);
        TogglePanelState(industrialShopPanel, true);

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.InventoryPanel != null)
        {
            TogglePanelState(PlayerUIManager.Instance.InventoryPanel, true, false);
        }
    }

    public void OpenTrashDisposal()
    {
        currentShopType = ActiveShopType.TrashDisposal;
        TogglePanelState(trashDisposalPanel, true);

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.InventoryPanel != null)
        {
            TogglePanelState(PlayerUIManager.Instance.InventoryPanel, true, false);
        }
    }

    public void HideShopPanel()
    {
        TogglePanelState(shopPanel, false);
        TogglePanelState(industrialShopPanel, false, false);
        TogglePanelState(trashDisposalPanel, false, false);

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.InventoryPanel != null)
        {
            TogglePanelState(PlayerUIManager.Instance.InventoryPanel, false, false);
        }

        currentShopType = ActiveShopType.None;
    }

    public void ToggleShop()
    {
        bool target = !shopPanel.activeSelf;
        TogglePanelState(shopPanel, target);

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.InventoryPanel != null)
        {
            TogglePanelState(PlayerUIManager.Instance.InventoryPanel, target, false);
        }

        if (!target) currentShopType = ActiveShopType.None;
    }

    // ---------------------------
    // CRAFTING & QUESTS
    // ---------------------------
    public void ShowCraftingPanel() => TogglePanelState(craftingPanel, true);
    public void HideCraftingPanel() => TogglePanelState(craftingPanel, false);
    public void ToggleCrafting() => TogglePanelState(craftingPanel);
    public void ToggleQuest() => TogglePanelState(questPanel);

    public void ToggleFishIndex()
    {
        if (fishIndexPanel == null)
            return;

        RefreshFishIndex();
        TogglePanelState(fishIndexPanel);
    }

    public void RefreshFishIndex(ItemData caughtItem = null)
    {
        if (fishIndexGrid == null)
            return;

        foreach (Transform child in fishIndexGrid)
        {
            Destroy(child.gameObject);
        }

        if (FishIndex.Instance == null)
            FishIndex.EnsureInstance();

        IReadOnlyList<FishData> fishList = FishIndex.Instance != null ? FishIndex.Instance.GetAllFish() : null;
        if (fishList == null)
            return;

        foreach (FishData fish in fishList)
        {
            if (fish == null)
                continue;

            GameObject entry = fishIndexEntryPrefab != null
                ? Instantiate(fishIndexEntryPrefab, fishIndexGrid)
                : new GameObject(fish.itemName, typeof(RectTransform));

            RectTransform entryRect = entry.GetComponent<RectTransform>();
            if (entryRect != null)
            {
                entryRect.SetParent(fishIndexGrid, false);
            }

            Image background = entry.GetComponent<Image>();
            if (background == null)
            {
                background = entry.AddComponent<Image>();
                background.color = new Color(1f, 1f, 1f, 0.15f);
            }

            Transform labelTransform = entry.transform.Find("Label");
            TextMeshProUGUI label = labelTransform != null ? labelTransform.GetComponent<TextMeshProUGUI>() : null;
            if (label == null)
            {
                GameObject textObj = new GameObject("Label", typeof(RectTransform));
                textObj.transform.SetParent(entry.transform, false);
                label = textObj.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 18;
                label.color = Color.white;
                label.rectTransform.anchorMin = new Vector2(0f, 0f);
                label.rectTransform.anchorMax = new Vector2(1f, 1f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
            }

            Transform iconTransform = entry.transform.Find("Icon");
            Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (icon == null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(entry.transform, false);
                icon = iconObj.AddComponent<Image>();
                icon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                icon.rectTransform.sizeDelta = new Vector2(32f, 32f);
                icon.rectTransform.anchoredPosition = new Vector2(18f, 0f);
            }

            bool unlocked = FishIndex.Instance.IsFishUnlocked(fish);
            if (unlocked)
            {
                icon.sprite = fish.icon;
                icon.enabled = fish.icon != null;
                icon.color = Color.white;
                label.text = fish.itemName;
                label.color = Color.white;
                background.color = new Color(0.2f, 0.6f, 0.25f, 0.75f);
            }
            else
            {
                icon.sprite = fish.icon != null ? fish.icon : fishIndexLockedSprite;
                icon.enabled = icon.sprite != null;
                icon.color = Color.black;
                label.text = "????";
                label.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
                background.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            }
        }
    }

    public void CloseAllStandardPanels()
    {
        if ((dialoguePanel != null && dialoguePanel.activeSelf) ||
            (cutscenePanel != null && cutscenePanel.activeSelf)) return;

        bool closedSomething = false;

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.InventoryPanel != null && PlayerUIManager.Instance.InventoryPanel.activeSelf)
        {
            TogglePanelState(PlayerUIManager.Instance.InventoryPanel, false);
            closedSomething = true;
        }

        if (craftingPanel != null && craftingPanel.activeSelf) { TogglePanelState(craftingPanel, false); closedSomething = true; }
        if (questPanel != null && questPanel.activeSelf) { TogglePanelState(questPanel, false); closedSomething = true; }
        if (fishIndexPanel != null && fishIndexPanel.activeSelf) { TogglePanelState(fishIndexPanel, false); closedSomething = true; }
        if (choicePanel != null && choicePanel.activeSelf) { HideChoicePanel(); closedSomething = true; }
        if (cleaningPanel != null && cleaningPanel.activeSelf) { TogglePanelState(cleaningPanel, false); closedSomething = true; }
        if (seaweedPanel != null && seaweedPanel.activeSelf) { TogglePanelState(seaweedPanel, false); closedSomething = true; }

        if ((shopPanel != null && shopPanel.activeSelf) ||
            (industrialShopPanel != null && industrialShopPanel.activeSelf) ||
            (trashDisposalPanel != null && trashDisposalPanel.activeSelf))
        {
            HideShopPanel();
            closedSomething = true;
        }

        if (closedSomething) TooltipUI.Instance?.HideTooltip();
    }

    // ---------------------------
    // MESSAGES & CHOICE POPUPS
    // ---------------------------
    public void ShowMessage(string msg)
    {
        if (messageText == null) return;
        messageText.text = msg;
    }

    public void ShowChoice(string prompt, string opt1Text, Action opt1Action, string opt2Text, Action opt2Action)
    {
        if (choicePanel == null || throwInOceanButton == null || cleanUpButton == null) return;

        if (choicePromptText != null) choicePromptText.text = prompt;
        if (throwInOceanText != null) throwInOceanText.text = opt1Text;
        if (cleanUpText != null) cleanUpText.text = opt2Text;

        throwInOceanButton.onClick.RemoveAllListeners();
        cleanUpButton.onClick.RemoveAllListeners();

        throwInOceanButton.onClick.AddListener(() => {
            choicePanel.SetActive(false);
            opt1Action?.Invoke();
            CheckAndRestoreState();
        });

        cleanUpButton.onClick.AddListener(() => {
            choicePanel.SetActive(false);
            opt2Action?.Invoke();
            CheckAndRestoreState();
        });

        TogglePanelState(choicePanel, true);
    }

    public void HideChoicePanel() => TogglePanelState(choicePanel, false);

    private void CheckAndRestoreState()
    {
        if (!IsUIOpen() && (GameManager.Instance == null || !GameManager.Instance.IsTransitioningDay))
        {
            GameManager.Instance?.SetState(GameState.Normal);
            PlayerController.Instance?.UnlockMovement();
        }
    }

    public void PlayPanelOpenSFX() => GameEvents.TriggerSound(SoundType.PanelOpen);
    public void PlayPanelCloseSFX() => GameEvents.TriggerSound(SoundType.PanelClose);

    public bool IsPointerOverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    public bool IsUIOpen()
    {
        GameObject invPanel = PlayerUIManager.Instance != null ? PlayerUIManager.Instance.InventoryPanel : null;

        return (invPanel != null && invPanel.activeSelf) ||
               (craftingPanel != null && craftingPanel.activeSelf) ||
               (shopPanel != null && shopPanel.activeSelf) ||
               (industrialShopPanel != null && industrialShopPanel.activeSelf) ||
               (trashDisposalPanel != null && trashDisposalPanel.activeSelf) ||
               (questPanel != null && questPanel.activeSelf) ||
               (fishIndexPanel != null && fishIndexPanel.activeSelf) ||
               (choicePanel != null && choicePanel.activeSelf) ||
               (cleaningPanel != null && cleaningPanel.activeSelf) ||
               (seaweedPanel != null && seaweedPanel.activeSelf) ||
               (cutscenePanel != null && cutscenePanel.activeSelf) ||
               (dialoguePanel != null && dialoguePanel.activeSelf);
    }
}