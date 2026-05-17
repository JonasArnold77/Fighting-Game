using UnityEngine;

/// <summary>
/// Beschreibt einen einzelnen Kampf-Move vollständig.
/// Wird als ScriptableObject angelegt: Rechtsklick → Create → Fighting Game → Move Data.
/// </summary>
[CreateAssetMenu(fileName = "NewMove", menuName = "Fighting Game/Move Data")]
public class MoveData : ScriptableObject
{
    [Header("Identität")]
    /// <summary>Anzeigename des Moves (z.B. "Linker Haken").</summary>
    public string moveName;

    /// <summary>Icon für UI-Darstellung.</summary>
    public Sprite icon;

    [Header("Kampf")]
    /// <summary>Körperteil des Angreifers, der den Move ausführt.</summary>
    public BodyPart bodyPart;

    /// <summary>Zone am Gegner, die dieser Move trifft.</summary>
    public TargetZone targetZone;

    /// <summary>Zone, die der Charakter beherrschen muss, um diesen Move zu lernen.</summary>
    public TargetZone learnZone;

    /// <summary>Basis-Schadenswert des Moves.</summary>
    public int damage;

    /// <summary>Rolle des Moves in einer Combo-Kette.</summary>
    public ComboType comboType;

    /// <summary>Magische Elemente, die auf diesen Move wirken (optional, mehrere möglich).</summary>
    public MagicTag[] magicTags;

    /// <summary>Maximale Distanz (in Metern) zwischen Angreifer und Ziel, damit ein Treffer registriert wird.</summary>
    [Tooltip("Maximale Distanz zum Gegner für einen Treffer. Typisch: 1.0–2.0 m.")]
    public float attackRange = 1.5f;

    /// <summary>Zieldistanz zum Gegner, zu der beim Ausführen hingelerpt wird. 0 = kein Step.</summary>
    [Tooltip("Distanz zum Gegner zu der beim Angriff hingelerpt wird (m). 0 = kein Step.")]
    public float attackStepDistance = 0f;

    /// <summary>Geschwindigkeit des Attack-Steps in m/s.</summary>
    [Tooltip("Wie schnell zum Gegner hingelerpt wird (m/s).")]
    public float attackStepSpeed = 5f;

    [Header("Animation")]
    /// <summary>AnimationClip, der für diesen Move abgespielt wird.</summary>
    public AnimationClip animationClip;

    /// <summary>Abspielgeschwindigkeit der Animation (1.0 = normal).</summary>
    [Range(0.1f, 3f)]
    public float animationSpeed = 1f;

    /// <summary>Wie lange die Hitbox dieses Moves aktiv bleibt (in Sekunden).</summary>
    public float hitboxActiveTime = 0.2f;

    /// <summary>Geschwindigkeit mit der IK die Hand/Fuß zum Ziel zieht (Weight-Lerp pro Sekunde). 0 = kein IK.</summary>
    [Tooltip("Wie schnell IK die Hand/Fuß zum Ziel zieht. 0 = kein IK. Niedrig = weich, Hoch = hart.")]
    [Range(0f, 30f)]
    public float ikWeightSpeed = 0f;
}
