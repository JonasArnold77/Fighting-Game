using System.Collections;
using UnityEngine;

/// <summary>
/// Lässt das getroffene Körperteil kurz aufleuchten.
/// Spawnt eine kleine Kugel mit emissivem Material am Bone der getroffenen Zone
/// und blendet sie aus.
///
/// Setup:
///   1. Komponente auf den Fighter-Root ziehen.
///   2. HurtboxZoneRegistry wird automatisch im Hierarchy gesucht.
/// </summary>
public class HitZoneFlash : MonoBehaviour
{
    [Header("Flash Sphere")]
    [Tooltip("Farbe des Aufblitzens.")]
    [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0f);

    [Tooltip("Maximale Emission-Intensität.")]
    [SerializeField] private float flashIntensity = 6f;

    [Tooltip("Größe der Kugel (m).")]
    [SerializeField] private float sphereSize = 0.08f;

    [Tooltip("Wie lange die Kugel ausgeblendet wird (s).")]
    [SerializeField] private float flashDuration = 0.2f;

    // -------------------------------------------------------------------------

    private HurtboxZoneRegistry zoneRegistry;
    private Coroutine           flashCoroutine;

    private void Awake()
    {
        zoneRegistry = GetComponentInChildren<HurtboxZoneRegistry>();

        if (zoneRegistry == null)
            zoneRegistry = GetComponentInParent<HurtboxZoneRegistry>();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Spawnt eine aufleuchtende Kugel an der getroffenen Zone.
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
        // Kugel erstellen
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = Vector3.one * sphereSize;

        // Collider entfernen damit keine Physik-Konflikte entstehen
        Destroy(sphere.GetComponent<Collider>());

        // Shader automatisch erkennen (URP oder Built-in)
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Standard");

        Material mat  = new Material(shader);
        bool     isUrp = shader.name.Contains("Universal");

        if (isUrp)
        {
            mat.SetColor("_BaseColor", flashColor * flashIntensity);
        }
        else
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_Color",         flashColor);
            mat.SetColor("_EmissionColor", flashColor * flashIntensity);
        }

        sphere.GetComponent<Renderer>().material = mat;

        float timer = 0f;

        while (timer < flashDuration)
        {
            // Bone-Position jeden Frame folgen
            if (bone != null)
                sphere.transform.position = bone.position;

            // Farbe ausfaden
            float t         = timer / flashDuration;
            float intensity = Mathf.Lerp(flashIntensity, 0f, t);
            Color current   = flashColor * intensity;

            if (isUrp)
                mat.SetColor("_BaseColor", current);
            else
                mat.SetColor("_EmissionColor", current);

            // Kugel gleichzeitig schrumpfen lassen
            float scale = Mathf.Lerp(sphereSize, 0f, t);
            sphere.transform.localScale = Vector3.one * scale;

            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(mat);
        Destroy(sphere);
        flashCoroutine = null;
    }
}
