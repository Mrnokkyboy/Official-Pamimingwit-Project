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
    [Min(1)] public int seaweedMinGrowthDays = 2;
    [Min(1)] public int seaweedMaxGrowthDays = 4;
    [Tooltip("Positive for sustainable farms, negative for illegal cages.")]
    public int sustainabilityEffect = 0;

    [Header("Visuals")]
    public Sprite seedlingSprite;
    public Sprite growingSprite;
    public Sprite readySprite;
    [Header("Coral States")]
    public Sprite coralClearSprite;
    public Sprite coralInfestedSprite;
    [Min(1)] public int coralMinGrowthDays = 2;
    [Min(1)] public int coralMaxGrowthDays = 4;
    [Min(1)] public int coralMinInfestationDays = 1;
    [Min(1)] public int coralMaxInfestationDays = 3;
    [Min(0.01f)] public float coralDamageToBreak = 5f;
    
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
    private bool coralInfested;
    private float coralDamage;
    private int seaweedReadyDay;
    private int seaweedGrowthDays;
    private int coralReadyDay;
    private int coralGrowthDays;
    private int coralInfestationDay;
    private Vector3 basePosition;
    private bool isInWater;
    private float rippleTimer;
    private bool isSeaweed => deployableType == DeployableType.Seaweed;
    private bool isCoral => deployableType == DeployableType.Coral;
    private bool isGrowingFarm => isSeaweed || isCoral;
    public bool IsReadyCoral => isCoral && isReady;
    public bool IsCoralInfested => isCoral && isReady && coralInfested;
    public float CoralDamage => coralDamage;
    public Sprite CoralInfestedSprite => coralInfestedSprite != null ? coralInfestedSprite : readySprite;
    public Sprite CoralClearSprite => coralClearSprite != null ? coralClearSprite : growingSprite;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = isGrowingFarm && seedlingSprite != null ? seedlingSprite : growingSprite;
        }

        basePosition = transform.position;

        if (isSeaweed) SetSeaweedReadyDay();
        if (isCoral) SetCoralReadyDay();

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
            if (isCoral && !coralInfested && GameManager.Instance != null &&
                GameManager.Instance.currentDay >= coralInfestationDay)
            {
                SetCoralInfested();
            }

            if (spawnedIndicator != null)
            {
                float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                spawnedIndicator.transform.localScale = new Vector3(s, s, 1f);
            }
            return;
        }

        if (isSeaweed || isCoral)
        {
            int readyDay = isSeaweed ? seaweedReadyDay : coralReadyDay;
            int growthDays = isSeaweed ? seaweedGrowthDays : coralGrowthDays;
            if (GameManager.Instance != null && GameManager.Instance.currentDay >= readyDay)
            {
                isReady = true;
                if (isCoral)
                {
                    coralInfested = false;
                    ScheduleCoralInfestation();
                    if (sr != null) sr.sprite = CoralClearSprite;
                }
                else if (sr != null && readySprite != null)
                {
                    sr.sprite = readySprite;
                }

                if (!isCoral) SpawnReadyIndicator();
            }
            else if (sr != null && growingSprite != null &&
                GameManager.Instance != null &&
                GameManager.Instance.currentDay > readyDay - Mathf.Max(1, Mathf.CeilToInt(growthDays * 0.34f)))
            {
                sr.sprite = growingSprite;
            }
        }
        else
        {
            timer += Time.deltaTime;
            if (timer >= readyTime)
            {
                isReady = true;
                if (sr != null && readySprite != null)
                {
                    sr.sprite = readySprite;
                }

                SpawnReadyIndicator();
            }
        }
    }

    private static int GetRandomDays(int minimum, int maximum)
    {
        int min = Mathf.Max(1, minimum);
        int max = Mathf.Max(min, maximum);
        return Random.Range(min, max + 1);
    }

    private void SetCoralReadyDay()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[HarvestableDeployable] GameManager instance missing; coral cannot track growth days.");
            coralReadyDay = int.MaxValue;
            return;
        }

        coralGrowthDays = GetRandomDays(coralMinGrowthDays, coralMaxGrowthDays);
        coralReadyDay = GameManager.Instance.currentDay + coralGrowthDays;
    }

    private void ScheduleCoralInfestation()
    {
        coralInfestationDay = GameManager.Instance.currentDay +
            GetRandomDays(coralMinInfestationDays, coralMaxInfestationDays);
    }

    private void SetSeaweedReadyDay()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[HarvestableDeployable] GameManager instance missing; seaweed cannot track growth days.");
            seaweedReadyDay = int.MaxValue;
            return;
        }

        int minimumDays = Mathf.Max(1, seaweedMinGrowthDays);
        int maximumDays = Mathf.Max(minimumDays, seaweedMaxGrowthDays);
        seaweedGrowthDays = Random.Range(minimumDays, maximumDays + 1);
        seaweedReadyDay = GameManager.Instance.currentDay + seaweedGrowthDays;
    }

    private void SpawnReadyIndicator()
    {
        if (readyIndicatorPrefab != null && spawnedIndicator == null)
        {
            spawnedIndicator = Instantiate(readyIndicatorPrefab, transform.position + indicatorOffset, Quaternion.identity);
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

    public bool CompleteCoralCare(int harvestAmount)
    {
        if (!IsReadyCoral) return false;

        bool anyAdded = false;
        for (int i = 0; i < harvestAmount; i++)
        {
            ItemData caught = GetHarvestItem(false);
            if (caught != null && Inventory.Instance != null &&
                Inventory.Instance.AddItem(caught, 1, FishQuality.Bronze))
            {
                anyAdded = true;
            }
        }
        coralInfested = false;
        ScheduleCoralInfestation();
        if (sr != null) sr.sprite = CoralClearSprite;
        if (spawnedIndicator != null)
        {
            Destroy(spawnedIndicator);
            spawnedIndicator = null;
        }
        if (sustainabilityEffect != 0) SustainabilityManager.Instance?.Add(sustainabilityEffect);

        UIManager.Instance?.ShowMessage(anyAdded ? "Coral cared for and fragments harvested!" : "Coral cleared, but your inventory is full!");
        return anyAdded;
    }

    private void SetCoralInfested()
    {
        coralInfested = true;
        if (sr != null) sr.sprite = CoralInfestedSprite;
        if (readyIndicatorPrefab != null && spawnedIndicator == null)
        {
            spawnedIndicator = Instantiate(readyIndicatorPrefab, transform.position + indicatorOffset, Quaternion.identity);
        }
    }

    public bool AddCoralDamage(float damage, float breakThreshold)
    {
        if (!isCoral || damage <= 0f || !isReady) return false;

        coralDamage = Mathf.Min(coralDamage + damage, breakThreshold);
        if (coralDamage < breakThreshold) return false;

        if (spawnedIndicator != null) Destroy(spawnedIndicator);
        Destroy(gameObject);
        return true;
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
        if (isSeaweed) SetSeaweedReadyDay();
        if (isCoral) SetCoralReadyDay();
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
            if (isCoral)
            {
                if (coralInfested) return "Clear Sea Urchins [E]";
                int daysUntilInfested = Mathf.Max(0, coralInfestationDay - GameManager.Instance.currentDay);
                return $"Care for Coral [E] (infested in {daysUntilInfested} day{(daysUntilInfested == 1 ? "" : "s")})";
            }
            return isSeaweed ? "Harvest Seaweed [E]" : $"Harvest {deployableType} [E]";
        }
        if (isGrowingFarm)
        {
            if (isSeaweed && GameManager.Instance != null)
            {
                int daysRemaining = Mathf.Max(0, seaweedReadyDay - GameManager.Instance.currentDay);
                string seaweedStage = daysRemaining == seaweedGrowthDays ? "Seedling" : "Growing";
                return $"{seaweedStage}... ({daysRemaining} day{(daysRemaining == 1 ? "" : "s")})";
            }

            if (isCoral && GameManager.Instance != null)
            {
                int daysRemaining = Mathf.Max(0, coralReadyDay - GameManager.Instance.currentDay);
                string coralStage = daysRemaining == coralGrowthDays ? "Coral polyp" : "Coral growing";
                return $"{coralStage}... ({daysRemaining} day{(daysRemaining == 1 ? "" : "s")})";
            }

            string stage = timer < readyTime * 0.34f
                ? (isCoral ? "Coral polyp" : "Seedling")
                : (isCoral ? "Coral growing" : "Growing");
            return $"{stage}... ({Mathf.Ceil(readyTime - timer)}s)";
        }
        return $"Growing... ({Mathf.Ceil(readyTime - timer)}s)";
    }
}