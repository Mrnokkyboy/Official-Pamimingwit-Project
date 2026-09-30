using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class FishShadowPoolSpawner : MonoBehaviour
{
    private sealed class ShadowPool
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector3 driftDirection;
        public float driftSpeed;
        public float respawnTime;
    }

    private const int PoolCount = 4;
    private const int PositionAttempts = 120;
    private const float MinimumRespawnTime = 8f;
    private const float MaximumRespawnTime = 15f;

    [Header("Water Placement")]
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private int poolCount = PoolCount;
    [SerializeField] private int positionAttempts = PositionAttempts;

    [Header("Pool Appearance")]
    [SerializeField] private string sortingLayerName = "WalkInfront";
    [SerializeField] private int sortingOrder = 10;
    [SerializeField] private Color poolColor = new Color(0.2f, 0.95f, 1f, 1f);
    [SerializeField] private Vector2 poolScaleRange = new Vector2(1.1f, 1.65f);

    [Header("Pool Movement")]
    [SerializeField] private Vector2 driftSpeedRange = new Vector2(0.025f, 0.07f);
    [SerializeField] private Vector2 respawnTimeRange = new Vector2(MinimumRespawnTime, MaximumRespawnTime);

    public static FishShadowPoolSpawner Instance { get; private set; }

    private readonly List<ShadowPool> pools = new List<ShadowPool>();
    private Sprite poolSprite;
    private Texture2D poolTexture;
    private bool initialized;

    private void Awake()
    {
        if (initialized) return;

        initialized = true;
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[FishShadowPoolSpawner] More than one fish-shadow pool spawner is active.");
            enabled = false;
            return;
        }

        Instance = this;
        if (mainCamera == null) mainCamera = Camera.main;
        poolTexture = CreatePoolTexture();
        poolSprite = Sprite.Create(poolTexture, new Rect(0, 0, poolTexture.width, poolTexture.height), new Vector2(0.5f, 0.5f), 64f);
        poolSprite.name = "Fish Shadow Pool";
        StartCoroutine(SpawnPools());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (poolSprite != null) Destroy(poolSprite);
        if (poolTexture != null) Destroy(poolTexture);
    }

    private void Update()
    {
        if (!initialized) return;

        float deltaTime = Time.deltaTime;
        foreach (ShadowPool pool in pools)
        {
            if (pool.transform == null) continue;

            Vector3 nextPosition = pool.transform.position + pool.driftDirection * (pool.driftSpeed * deltaTime);
            if (Physics2D.OverlapCircle(nextPosition, 0.1f, waterLayer) != null)
            {
                pool.transform.position = nextPosition;
            }
            else
            {
                pool.driftDirection = Random.insideUnitCircle.normalized;
            }

            pool.respawnTime -= deltaTime;
            if (pool.respawnTime <= 0f)
            {
                StartCoroutine(RelocatePool(pool));
                pool.respawnTime = float.PositiveInfinity;
            }
        }
    }

    public bool IsPositionInPool(Vector2 position)
    {
        foreach (ShadowPool pool in pools)
        {
            if (pool.transform == null || pool.renderer == null || !pool.renderer.enabled) continue;

            Bounds bounds = pool.renderer.bounds;
            float normalizedX = (position.x - bounds.center.x) / Mathf.Max(bounds.extents.x, 0.01f);
            float normalizedY = (position.y - bounds.center.y) / Mathf.Max(bounds.extents.y, 0.01f);
            if ((normalizedX * normalizedX) + (normalizedY * normalizedY) <= 1f)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator SpawnPools()
    {
        yield return null;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[FishShadowPoolSpawner] A main camera is required to place fish-shadow pools.");
            yield break;
        }
        if (waterLayer.value == 0)
        {
            Debug.LogError("[FishShadowPoolSpawner] Assign the water layer in the Inspector.");
            yield break;
        }

        for (int i = 0; i < Mathf.Max(0, poolCount); i++)
        {
            if (!TryFindWaterPosition(out Vector3 position)) continue;

            GameObject poolObject = new GameObject($"Fish Shadow Pool {i + 1}");
            poolObject.transform.SetParent(transform, true);
            poolObject.transform.position = position;

            SpriteRenderer spriteRenderer = poolObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = poolSprite;
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.color = poolColor;

            float scale = Random.Range(poolScaleRange.x, poolScaleRange.y);
            poolObject.transform.localScale = new Vector3(scale, scale * Random.Range(0.8f, 1.15f), 1f);

            ShadowPool pool = new ShadowPool
            {
                transform = poolObject.transform,
                renderer = spriteRenderer,
                driftDirection = Random.insideUnitCircle.normalized,
                driftSpeed = Random.Range(driftSpeedRange.x, driftSpeedRange.y),
                respawnTime = Random.Range(respawnTimeRange.x, respawnTimeRange.y)
            };
            pools.Add(pool);
        }
    }

    private IEnumerator RelocatePool(ShadowPool pool)
    {
        if (pool.transform == null || pool.renderer == null) yield break;

        Color startColor = pool.renderer.color;
        float elapsed = 0f;
        while (elapsed < 0.6f && pool.renderer != null)
        {
            elapsed += Time.deltaTime;
            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, elapsed / 0.6f);
            pool.renderer.color = color;
            yield return null;
        }

        if (pool.transform != null && TryFindWaterPosition(out Vector3 position))
        {
            pool.transform.position = position;
            pool.driftDirection = Random.insideUnitCircle.normalized;
            pool.respawnTime = Random.Range(respawnTimeRange.x, respawnTimeRange.y);
        }
        else if (pool.transform != null)
        {
            pool.respawnTime = 2f;
        }

        elapsed = 0f;
        while (elapsed < 0.6f && pool.renderer != null)
        {
            elapsed += Time.deltaTime;
            Color color = startColor;
            color.a = Mathf.Lerp(0f, startColor.a, elapsed / 0.6f);
            pool.renderer.color = color;
            yield return null;
        }

        if (pool.renderer != null) pool.renderer.color = startColor;
    }

    private bool TryFindWaterPosition(out Vector3 position)
    {
        position = Vector3.zero;
        if (mainCamera == null) return false;

        float height = mainCamera.orthographic ? mainCamera.orthographicSize : 5f;
        float width = height * mainCamera.aspect;
        Vector3 cameraPosition = mainCamera.transform.position;

        for (int attempt = 0; attempt < Mathf.Max(1, positionAttempts); attempt++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(cameraPosition.x - width, cameraPosition.x + width),
                Random.Range(cameraPosition.y - height, cameraPosition.y + height),
                0f);

            if (Physics2D.OverlapCircle(candidate, 0.1f, waterLayer) != null)
            {
                position = candidate;
                return true;
            }
        }

        return false;
    }

    private static Texture2D CreatePoolTexture()
    {
        const int width = 96;
        const int height = 56;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Fish Shadow Pool Texture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2[] ellipseCenters =
        {
            new Vector2(0.5f, 0.5f),
            new Vector2(0.29f, 0.55f),
            new Vector2(0.7f, 0.43f)
        };
        Vector2[] ellipseRadii =
        {
            new Vector2(0.36f, 0.34f),
            new Vector2(0.23f, 0.24f),
            new Vector2(0.22f, 0.25f)
        };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 point = new Vector2((x + 0.5f) / width, (y + 0.5f) / height);
                float edgeDistance = float.PositiveInfinity;
                for (int i = 0; i < ellipseCenters.Length; i++)
                {
                    Vector2 offset = point - ellipseCenters[i];
                    float distance = (offset.x * offset.x) / (ellipseRadii[i].x * ellipseRadii[i].x) +
                        (offset.y * offset.y) / (ellipseRadii[i].y * ellipseRadii[i].y);
                    edgeDistance = Mathf.Min(edgeDistance, distance);
                }

                float alpha = 1f - Mathf.SmoothStep(0.82f, 1.08f, edgeDistance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }
}
