using UnityEngine;

public enum DeployableType { Cage, Farm, Coral, Seaweed }

public class HarvestableDeployable : MonoBehaviour, IInteractable
{
    [Header("Harvest Settings")]
    public DeployableType deployableType;
    public ItemData resultItem;
    [Tooltip("Items randomly awarded when this deployable is successfully harvested.")]
    public ItemData[] harvestPool;
    public int amount = 4;
    public float readyTime = 60f;
    [Tooltip("Positive for sustainable farms, negative for illegal cages.")]
    public int sustainabilityEffect = 0;

    [Header("Visuals")]
    public Sprite seedlingSprite;
    public Sprite growingSprite;
    public Sprite readySprite;
    
    [Header("Juice - General")]
    public GameObject readyIndicatorPrefab;
    private GameObject spawnedIndicator;
    public Vector3 indicatorOffset = new Vector3(0, 1.2f, 0);
    public float pulseSpeed = 5f;
    public float pulseAmount = 0.15f;

    [Header("Juice - Water")]
    public GameObject ripplePrefab;
    public float bobSpeed = 2f;
    public float bobAmount = 0.05f;
    public float rippleInterval = 2f;

    private SpriteRenderer sr;
    private float timer;
    private bool isReady;
    private Vector3 basePosition;
    private bool isInWater;
    private float rippleTimer;
    private bool isSeaweed => deployableType == DeployableType.Seaweed;
    private bool isCoral => deployableType == DeployableType.Coral;
    private bool isGrowingFarm => isSeaweed || isCoral;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = isGrowingFarm && seedlingSprite != null ? seedlingSprite : growingSprite;
        }

        basePosition = transform.position;

        if (FishingManager.Instance != null)
        {
            isInWater = Physics2D.OverlapCircle(transform.position, 0.1f, FishingManager.Instance.waterLayer);
        }
    }

    private void Update()
    {
        if (isInWater)
        {
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.position = basePosition + new Vector3(0, yOffset, 0);

            rippleTimer += Time.deltaTime;
            if (rippleTimer >= rippleInterval)
            {
                rippleTimer = 0;
                if (ripplePrefab != null)
                {
                    Instantiate(ripplePrefab, basePosition, Quaternion.identity);
                }
            }
        }

        if (isReady)
        {
            if (spawnedIndicator != null)
            {
                float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                spawnedIndicator.transform.localScale = new Vector3(s, s, 1f);
            }
            return;
        }

        timer += Time.deltaTime;
        if (timer >= readyTime)
        {
            isReady = true;
            if (sr != null && readySprite != null) sr.sprite = readySprite;
            
            if (readyIndicatorPrefab != null && spawnedIndicator == null)
            {
                spawnedIndicator = Instantiate(readyIndicatorPrefab, transform.position + indicatorOffset, Quaternion.identity);
            }
        }
        else if (isGrowingFarm && sr != null && growingSprite != null && timer >= readyTime * 0.34f)
        {
            sr.sprite = growingSprite;
        }
    }

    public void Interact()
    {
        if (!isReady) return;

        if (isGrowingFarm)
        {
            if (isCoral)
            {
                if (CoralHarvestMinigame.Instance != null)
                {
                    CoralHarvestMinigame.Instance.StartGame(this);
                }
                else
                {
                    Debug.LogError("[HarvestableDeployable] CoralHarvestMinigame instance missing in scene!");
                }
            }
            else
            {
                if (SeaweedHarvestMinigame.Instance != null)
                {
                    SeaweedHarvestMinigame.Instance.StartGame(this);
                }
                else
                {
                    Debug.LogError("[HarvestableDeployable] SeaweedHarvestMinigame instance missing in scene!");
                }
            }
            return;
        }

        CompleteHarvest(Random.Range(1, 5), true);
    }

    public bool CompleteHarvest(int harvestAmount, bool useOceanCatch = false)
    {
        if (!isReady) return false;

        bool anyAdded = false;
        for (int i = 0; i < harvestAmount; i++)
        {
            ItemData caught = GetHarvestItem(useOceanCatch);
            if (caught == null && useOceanCatch) caught = resultItem;

            if (caught != null && Inventory.Instance != null &&
                Inventory.Instance.AddItem(caught, 1, FishQuality.Bronze))
            {
                anyAdded = true;
            }
        }

        if (anyAdded)
        {
            if (sustainabilityEffect != 0) SustainabilityManager.Instance?.Add(sustainabilityEffect);
            UIManager.Instance?.ShowMessage($"{deployableType} haul harvested!");
            ResetGrowth();
            return true;
        }
        else
        {
            UIManager.Instance?.ShowMessage("Inventory Full!");
            return false;
        }
    }

    private ItemData GetHarvestItem(bool useOceanCatch)
    {
        if (harvestPool != null && harvestPool.Length > 0)
        {
            ItemData pooledItem = harvestPool[Random.Range(0, harvestPool.Length)];
            if (pooledItem != null) return pooledItem;
        }

        if (useOceanCatch && ReactiveOceanManager.Instance != null)
        {
            ItemData oceanCatch = ReactiveOceanManager.Instance.GetRandomCatch();
            if (oceanCatch != null) return oceanCatch;
        }

        return resultItem;
    }

    public void ConsumeAfterHarvest()
    {
        Destroy(gameObject);
    }

    public void ResetGrowth()
    {
        isReady = false;
        timer = 0;
        if (sr != null)
        {
            sr.sprite = isGrowingFarm && seedlingSprite != null ? seedlingSprite : growingSprite;
        }

        if (spawnedIndicator != null)
        {
            Destroy(spawnedIndicator);
            spawnedIndicator = null;
        }
    }

    public string GetInteractPrompt()
    {
        if (isReady)
        {
            if (isCoral) return "Clear Sea Urchins [E]";
            return isSeaweed ? "Harvest Seaweed [E]" : $"Harvest {deployableType} [E]";
        }
        if (isGrowingFarm)
        {
            string stage = timer < readyTime * 0.34f
                ? (isCoral ? "Coral polyp" : "Seedling")
                : (isCoral ? "Coral growing" : "Growing");
            return $"{stage}... ({Mathf.Ceil(readyTime - timer)}s)";
        }
        return $"Growing... ({Mathf.Ceil(readyTime - timer)}s)";
    }
}