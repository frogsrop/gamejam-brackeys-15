using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

public class GhostMovement : MonoBehaviour
{
    [Header("Paths")]
    [Tooltip("Spline paths the ghost can travel along. Each SplineContainer holds one route.")]
    [SerializeField] private List<SplineContainer> paths = new List<SplineContainer>();

    [Header("Speed")]
    [SerializeField] private float baseSpeed = 2f;
    [Tooltip("Slowest the ghost drifts (multiplier on base speed).")]
    [SerializeField] private float minSpeedMultiplier = 0.08f;
    [Tooltip("Fastest normal drift (multiplier on base speed).")]
    [SerializeField] private float maxSpeedMultiplier = 1.6f;

    [Header("Ghostly Variation")]
    [Tooltip("How fast the underlying speed oscillates. Higher = more erratic.")]
    [SerializeField] private float noiseFrequency = 2f;
    [Tooltip("Probability per second of a sudden lurch forward.")]
    [SerializeField] private float lurchChance = 0.4f;
    [SerializeField] private float lurchSpeedMultiplier = 3.5f;
    [SerializeField] private float lurchDuration = 0.2f;
    [Tooltip("Probability per second of freezing in place for a moment.")]
    [SerializeField] private float freezeChance = 0.25f;
    [SerializeField] private float freezeDuration = 0.4f;

    [Header("Delay Between Paths")]
    [SerializeField] private float minDelay = 1f;
    [SerializeField] private float maxDelay = 4f;

    [Header("Listener Triggering")]
    [Tooltip("Probability (0-1) that the ghost triggers a Listener when flying over it.")]
    [Range(0f, 1f)]
    [SerializeField] private float triggerProbability = 0.5f;

    [Header("Facing")]
    [Tooltip("Flip the local X scale to face the movement direction (for 2D sprites).")]
    [SerializeField] private bool flipToFaceDirection = true;

    private float _noiseOffset;
    private bool _isLurching;
    private float _lurchTimer;
    private bool _isFrozen;
    private float _freezeTimer;

    void Start()
    {
        _noiseOffset = Random.Range(0f, 100f);
        StartCoroutine(GhostRoutine());
    }

    private IEnumerator GhostRoutine()
    {
        while (true)
        {
            if (paths.Count == 0)
            {
                yield return null;
                continue;
            }

            SplineContainer path = paths[Random.Range(0, paths.Count)];
            _noiseOffset = Random.Range(0f, 100f);
            _isLurching = false;
            _isFrozen = false;

            float splineLength = path.CalculateLength();
            float progress = 0f;

            while (progress < 1f)
            {
                UpdateGhostlyState();

                float speedMult = GetSpeedMultiplier();
                float delta = (baseSpeed * speedMult * Time.deltaTime) / Mathf.Max(splineLength, 0.001f);
                progress = Mathf.Min(progress + delta, 1f);

                path.Evaluate(progress, out float3 pos, out float3 tangent, out float3 _);
                transform.position = new Vector3(pos.x, pos.y, pos.z);

                if (flipToFaceDirection && math.lengthsq(tangent) > 0.001f)
                {
                    Vector3 s = transform.localScale;
                    float absX = Mathf.Abs(s.x);
                    transform.localScale = new Vector3(
                        tangent.x < 0 ? -absX : absX,
                        s.y, s.z);
                }

                yield return null;
            }

            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);
        }
    }

    private void UpdateGhostlyState()
    {
        if (_isLurching)
        {
            _lurchTimer -= Time.deltaTime;
            if (_lurchTimer <= 0f) _isLurching = false;
        }

        if (_isFrozen)
        {
            _freezeTimer -= Time.deltaTime;
            if (_freezeTimer <= 0f) _isFrozen = false;
        }

        if (!_isLurching && !_isFrozen)
        {
            if (Random.value < lurchChance * Time.deltaTime)
            {
                _isLurching = true;
                _lurchTimer = lurchDuration;
            }
            else if (Random.value < freezeChance * Time.deltaTime)
            {
                _isFrozen = true;
                _freezeTimer = freezeDuration;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var listener = other.GetComponent<Listener>();
        if (listener == null) return;

        if (!listener.alwaysTrigger && Random.value > triggerProbability) return;

        listener.Trigger();
    }

    private float GetSpeedMultiplier()
    {
        if (_isFrozen) return 0f;

        float t = Time.time * noiseFrequency + _noiseOffset;
        float noise = Mathf.PerlinNoise(t, _noiseOffset + 42f);
        float multiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, noise);

        if (_isLurching)
            multiplier *= lurchSpeedMultiplier;

        return multiplier;
    }
}
