using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CoralHarvestMinigame : MonoBehaviour
{
    public static CoralHarvestMinigame Instance { get; private set; }

    [SerializeField] private int seaUrchinCount = 5;
    [SerializeField] private GameObject coralPanel;
    [SerializeField] private RectTransform playArea;
    [SerializeField] private Image coralImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button seaUrchinPrefab;

    private readonly List<CoralUrchinTarget> activeUrchins = new List<CoralUrchinTarget>();
    private HarvestableDeployable currentCoral;
    private float coralRadius;
    private int removedUrchinCount;
    private bool active;
    private Coroutine closeCoroutine;

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
        if (active && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelGame();
        }
    }

    public void StartGame(HarvestableDeployable coral)
    {
        if (active || coral == null) return;
        if (!CanStartGame()) return;

        FishData seaUrchin = Resources.Load<FishData>("Fishes/SeaUrchin");
        if (seaUrchin == null || seaUrchin.icon == null)
        {
            Debug.LogError("[CoralHarvestMinigame] Could not load the Sea Urchin icon.");
            return;
        }

        currentCoral = coral;
        removedUrchinCount = 0;
        active = true;
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }

        coralImage.gameObject.SetActive(true);
        coralRadius = Mathf.Min(coralImage.rectTransform.rect.width, coralImage.rectTransform.rect.height) * 0.5f;
        ClearUrchins();
        coralImage.transform.SetAsFirstSibling();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.TogglePanelState(coralPanel, true);
        }
        else
        {
            coralPanel.SetActive(true);
        }

        titleText.text = "CORAL CARE";
        instructionText.text = "Drag each sea urchin completely off the coral!";
        resultText.text = string.Empty;
        UpdateProgress();
        SpawnUrchins(seaUrchin.icon);
    }

    private bool CanStartGame()
    {
        if (coralPanel != null && playArea != null && coralImage != null && titleText != null &&
            instructionText != null && progressText != null && resultText != null &&
            seaUrchinPrefab != null)
        {
            return true;
        }

        Debug.LogError("[CoralHarvestMinigame] Coral panel UI is not fully configured.");
        return false;
    }

    private void SpawnUrchins(Sprite seaUrchinSprite)
    {
        List<Vector2> positions = new List<Vector2>();
        float placementRadius = coralRadius * 0.62f;

        for (int i = 0; i < seaUrchinCount; i++)
        {
            Vector2 position = Random.insideUnitCircle * placementRadius;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                bool overlaps = false;
                foreach (Vector2 existingPosition in positions)
                {
                    if (Vector2.Distance(position, existingPosition) < 64f)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps) break;
                position = Random.insideUnitCircle * placementRadius;
            }

            positions.Add(position);
            Button button = Instantiate(seaUrchinPrefab, playArea);
            button.name = "SeaUrchin";
            button.gameObject.layer = playArea.gameObject.layer;

            RectTransform targetRect = button.GetComponent<RectTransform>();
            targetRect.anchorMin = new Vector2(0.5f, 0.5f);
            targetRect.anchorMax = new Vector2(0.5f, 0.5f);
            targetRect.pivot = new Vector2(0.5f, 0.5f);
            targetRect.anchoredPosition = position;
            targetRect.sizeDelta = new Vector2(64f, 64f);
            targetRect.localScale = Vector3.one;

            Image targetImage = button.GetComponent<Image>();
            if (targetImage != null)
            {
                targetImage.sprite = seaUrchinSprite;
                targetImage.preserveAspect = true;
                targetImage.raycastTarget = true;
            }

            CoralUrchinTarget target = button.gameObject.AddComponent<CoralUrchinTarget>();
            target.Initialize(this, coralRadius + 32f);
            activeUrchins.Add(target);
        }
    }

    public void OnUrchinDraggedOff(CoralUrchinTarget target)
    {
        if (!active || target == null || !activeUrchins.Remove(target)) return;

        removedUrchinCount++;
        Destroy(target.gameObject);
        UpdateProgress();

        if (removedUrchinCount >= seaUrchinCount)
        {
            FinishGame();
        }
    }

    private void UpdateProgress()
    {
        progressText.text = $"Sea urchins removed: {removedUrchinCount}/{seaUrchinCount}";
    }

    private void FinishGame()
    {
        active = false;
        ClearUrchins();

        bool harvested = currentCoral != null && currentCoral.CompleteHarvest(currentCoral.amount);
        if (harvested)
        {
            currentCoral.ConsumeAfterHarvest();
            resultText.text = "Coral cleared!";
        }
        else
        {
            resultText.text = "Inventory full. Coral is still ready.";
        }

        currentCoral = null;
        closeCoroutine = StartCoroutine(ClosePanelAfterDelay());
    }

    public void CancelGame()
    {
        if (!active) return;

        active = false;
        currentCoral = null;
        ClearUrchins();
        if (coralImage != null) coralImage.gameObject.SetActive(false);
        ClosePanel();
    }

    private IEnumerator ClosePanelAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1.2f);
        if (coralImage != null) coralImage.gameObject.SetActive(false);
        ClosePanel();
        closeCoroutine = null;
    }

    private void ClosePanel()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TogglePanelState(coralPanel, false);
        }
        else if (coralPanel != null)
        {
            coralPanel.SetActive(false);
        }
    }

    private void ClearUrchins()
    {
        for (int i = activeUrchins.Count - 1; i >= 0; i--)
        {
            if (activeUrchins[i] != null) Destroy(activeUrchins[i].gameObject);
        }

        activeUrchins.Clear();
    }
}
