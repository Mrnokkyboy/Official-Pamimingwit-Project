using System.Collections.Generic;
using UnityEngine;

public class CloudShadowSpawner : MonoBehaviour
{
    [Header("Cloud Prefabs")]
    [SerializeField] private GameObject[] cloudShadowPrefabs;

    [Header("Movement & Wind")]
    [SerializeField] private Vector2 windDirection = new Vector2(1f, 0.2f);
    [SerializeField] private float moveSpeed = 1.5f;

    [Header("Spawn Bounds (Camera Relative)")]
    [Tooltip("Half-extents of the camera view. For 1080p orthographic size ~5, use (12, 7).")]
    [SerializeField] private Vector2 spawnBounds = new Vector2(12f, 7f); 
    [SerializeField] private int maxClouds = 5;

    private Transform cameraTransform;
    private readonly List<GameObject> activeClouds = new List<GameObject>();

    private void Awake()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        WeatherManager.OnWeatherChanged += OnWeatherChanged;
        GameManager.OnDayAdvanced += RefreshWeatherState;

        RefreshWeatherState();
    }

    private void OnDisable()
    {
        WeatherManager.OnWeatherChanged -= OnWeatherChanged;
        GameManager.OnDayAdvanced -= RefreshWeatherState;

        ClearClouds();
    }

    private void Start()
    {
        RefreshWeatherState();
    }

    private void OnWeatherChanged(WeatherManager.WeatherState state)
    {
        RefreshWeatherState();
    }

    private void RefreshWeatherState()
    {
        bool isCloudy = WeatherManager.Instance != null && WeatherManager.Instance.CurrentWeather == WeatherManager.WeatherState.Cloudy;

        if (isCloudy)
        {
            if (activeClouds.Count == 0)
            {
                InitializeClouds();
            }
        }
        else
        {
            ClearClouds();
        }
    }

    private void Update()
    {
        if (cameraTransform == null) return;

        bool isCloudy = WeatherManager.Instance != null && WeatherManager.Instance.CurrentWeather == WeatherManager.WeatherState.Cloudy;
        if (!isCloudy)
        {
            if (activeClouds.Count > 0)
            {
                ClearClouds();
            }
            return;
        }

        if (activeClouds.Count == 0) return;

        MoveAndWrapClouds();
    }

    private void InitializeClouds()
    {
        ClearClouds();

        if (cloudShadowPrefabs == null || cloudShadowPrefabs.Length == 0) return;

        for (int i = 0; i < maxClouds; i++)
        {
            SpawnCloud(true);
        }
    }

    private void SpawnCloud(bool randomizeInitialPosition)
    {
        GameObject prefab = cloudShadowPrefabs[Random.Range(0, cloudShadowPrefabs.Length)];
        Vector3 spawnPos = cameraTransform.position;

        if (randomizeInitialPosition)
        {
            // Distribute evenly across viewport when state switches to Cloudy
            spawnPos.x += Random.Range(-spawnBounds.x, spawnBounds.x);
            spawnPos.y += Random.Range(-spawnBounds.y, spawnBounds.y);
        }
        else
        {
            // Position upwind off-screen for wrap-around
            spawnPos.x -= (spawnBounds.x + 2f) * Mathf.Sign(windDirection.x);
            spawnPos.y += Random.Range(-spawnBounds.y, spawnBounds.y);
        }

        spawnPos.z = 0f;

        GameObject cloud = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
        activeClouds.Add(cloud);
    }

    private void MoveAndWrapClouds()
    {
        Vector3 moveDelta = windDirection.normalized * moveSpeed * Time.deltaTime;

        for (int i = 0; i < activeClouds.Count; i++)
        {
            if (activeClouds[i] == null) continue;

            Transform cloudTr = activeClouds[i].transform;
            cloudTr.Translate(moveDelta, Space.World);

            Vector3 relativePos = cloudTr.position - cameraTransform.position;

            // Wrap position once past viewport boundary
            if (Mathf.Abs(relativePos.x) > spawnBounds.x + 3f)
            {
                float newX = cameraTransform.position.x - ((spawnBounds.x + 2f) * Mathf.Sign(windDirection.x));
                float newY = cameraTransform.position.y + Random.Range(-spawnBounds.y, spawnBounds.y);
                cloudTr.position = new Vector3(newX, newY, 0f);
            }
        }
    }

    private void ClearClouds()
    {
        for (int i = activeClouds.Count - 1; i >= 0; i--)
        {
            if (activeClouds[i] != null)
            {
                Destroy(activeClouds[i]);
            }
        }
        activeClouds.Clear();
    }

}