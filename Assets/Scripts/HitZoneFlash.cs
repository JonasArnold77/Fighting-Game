using System.Collections;
using UnityEngine;

/// <summary>
/// Lässt das getroffene Körperteil kurz aufleuchten.
/// Platziert ein Point Light am Bone der getroffenen TargetZone und blendet es aus.
///
/// Setup:
///   1. Komponente auf den Fighter-Root ziehen.
///   2. HurtboxZoneRegistry wird automatisch im Hierarchy gesucht.
/// </summary>
public class HitZoneFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [Tooltip("Farbe des Aufblitzens.")]
    [SerializeField] private Color  flashColor     = new Color(1f, 0.25f, 0f);

    [Tooltip("Maximale Lichtintensität beim Aufblitzen.")]
    [SerializeField] private float  flashIntensity = 4f;

    [Tooltip("Reichweite des Point Lights (m).")]
    [SerializeField] private float  flashRange     = 0.5f;

    [Tooltip("Wie lange das Licht ausgeblendet wird (s).")]
    [SerializeField] private float  flashDuration  = 0.2f;

    // -------------------------------------------------------------------------

    private HurtboxZoneRegistry zoneRegistry;
    private Light               flashLight;
    private Coroutine           flashCoroutine;

    private void Awake()
    {
        zoneRegistry = GetComponentInChildren<HurtboxZoneRegistry>();

        if (zoneRegistry == null)
            zoneRegistry = GetComponentInParent<HurtboxZoneRegistry>();

        // Point Light als Kind-Objekt erstellen
        GameObject lightGO = new GameObject("HitFlashLight");
        lightGO.transform.SetParent(transform);

        flashLight           = lightGO.AddComponent<Light>();
        flashLight.type      = LightType.Point;
        flashLight.color     = flashColor;
        flashLight.range     = flashRange;
        flashLight.intensity = 0f;
        flashLight.enabled   = false;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Lässt die getroffene Zone kurz aufleuchten.
    /// Wird von <see cref="Fighter.TakeDamage"/> aufgerufen.
    /// </summary>
    public void Flash(TargetZone zone)
    {
        Transform bone = zoneRegistry != null ? zoneRegistry.GetZoneTransform(zone) : null;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine(bone));
    }

    // -------------------------------------------------------------------------

    private IEnumerator FlashRoutine(Transform bone)
    {
        flashLight.color     = flashColor;
        flashLight.range     = flashRange;
        flashLight.intensity = flashIntensity;
        flashLight.enabled   = true;

        float timer = 0f;

        while (timer < flashDuration)
        {
            // Bone-Position jeden Frame folgen (bewegt sich mit der Animation)
            if (bone != null)
                flashLight.transform.position = bone.position;

            flashLight.intensity = Mathf.Lerp(flashIntensity, 0f, timer / flashDuration);

            timer += Time.deltaTime;
            yield return null;
        }

        flashLight.intensity = 0f;
        flashLight.enabled   = false;
        flashCoroutine       = null;
    }
}
