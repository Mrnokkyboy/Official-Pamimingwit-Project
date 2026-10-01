using UnityEngine;
using System;
using System.Collections;

public class FishingBobber : MonoBehaviour
{
    Vector3 startPos;
    float floatSpeed = 2f;
    float floatAmount = 0.1f;
    
    [Header("Flight Settings")]
    [SerializeField] private float flightSpeed = 3.5f;
    [SerializeField] private float flightArcHeight = 2.0f;
    [SerializeField] private GameObject ripplePrefab;

    [Header("Ambient Ripples")]
    [SerializeField] private float ambientRippleInterval = 1.5f;
    [SerializeField] private float ambientRippleScale = 0.5f;

    [Header("Intense Ripples (Biting/Reeling)")]
    [SerializeField] private float intenseRippleInterval = 0.4f;
    [SerializeField] private float intenseRippleScale = 0.8f;

    [Header("Juice Settings")]
    [SerializeField] private float shakeMagnitude = 0.12f;

    [Header("Audio")]
    [SerializeField] private AudioClip splashSFX;
    private AudioSource audioSource;

    public bool IsFlying { get; private set; }
    private Vector3 flightStart;
    private Vector3 flightTarget;
    private float flightProgress = 0;
    private Action onLand;
    private bool isBiting = false;
    private float ambientRippleTimer;
    private float intenseRippleTimer;

    private Coroutine nibbleCoroutine;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        startPos = transform.position;
    }

    void Update()
    {
        if (IsFlying)
        {
            UpdateFlight();
            return;
        }

        // Handle ambient ripples while waiting for a fish
        if (!isBiting)
        {
            ambientRippleTimer += Time.deltaTime;
            if (ambientRippleTimer >= ambientRippleInterval)
            {
                ambientRippleTimer = 0;
                SpawnRipple(ambientRippleScale);
            }
        }
        else
        {
            // Handle intense ripples while the fish is biting or being reeled
            intenseRippleTimer += Time.deltaTime;
            if (intenseRippleTimer >= intenseRippleInterval)
            {
                intenseRippleTimer = 0;
                SpawnRipple(intenseRippleScale);
            }
        }

        if (isBiting)
        {
            // Intense shake instead of rhythmic bobbing to provide frantic feedback
            Vector2 shake = UnityEngine.Random.insideUnitCircle * shakeMagnitude;
            transform.position = startPos + new Vector3(shake.x, shake.y - 0.1f, 0); // Keep it slightly submerged
        }
        else
        {
            float y = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            transform.position = startPos + new Vector3(0, y, 0);
        }
    }

    public void Launch(Vector3 target, Action callback)
    {
        flightStart = new Vector3(transform.position.x, transform.position.y, 0);
        flightTarget = new Vector3(target.x, target.y, 0);
        onLand = callback;
        flightProgress = 0;
        IsFlying = true;
    }

    private void UpdateFlight()
    {
        flightProgress += Time.deltaTime * flightSpeed;
        float distance = Vector3.Distance(flightStart, flightTarget);
        float normalizedProgress = flightProgress / Mathf.Max(distance, 0.1f);

        if (normalizedProgress >= 1.0f)
        {
            IsFlying = false;
            startPos = flightTarget; // Set the new resting position for bobbing
            transform.position = startPos;
            SpawnRipple(1.0f);
            if (splashSFX != null) audioSource.PlayOneShot(splashSFX);

            onLand?.Invoke();
            return;
        }

        float smoothedT = Mathf.SmoothStep(0f, 1f, normalizedProgress);

        Vector3 currentPos = Vector3.Lerp(flightStart, flightTarget, smoothedT);
        float height = Mathf.Sin(smoothedT * Mathf.PI) * flightArcHeight;
        currentPos.z = 0; 
        currentPos.y += height;
        transform.position = currentPos;
    }

    public void PlayNibble()
    {
        if (isBiting) return;

        SpawnRipple(0.65f);

        if (nibbleCoroutine != null) StopCoroutine(nibbleCoroutine);
        nibbleCoroutine = StartCoroutine(AnimateNibbleDip());
    }

    private IEnumerator AnimateNibbleDip()
    {
        Vector3 dipOffset = new Vector3(0, -0.15f, 0);
        float duration = 0.12f;
        float elapsed = 0f;

        Vector3 initialPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(initialPos, startPos + dipOffset, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos + dipOffset, startPos, elapsed / duration);
            yield return null;
        }
    }

    public void PlayBite()
    {
        if (nibbleCoroutine != null) StopCoroutine(nibbleCoroutine);
        isBiting = true;
    }

    private void SpawnRipple(float scaleMultiplier)
    {
        if (ripplePrefab == null) return;

        Quaternion randomRot = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));
        GameObject rippleGo = Instantiate(ripplePrefab, startPos, randomRot);

        if (rippleGo.TryGetComponent<RippleEffect>(out var ripple))
        {
            ripple.Initialize(scaleMultiplier);
        }
    }
}