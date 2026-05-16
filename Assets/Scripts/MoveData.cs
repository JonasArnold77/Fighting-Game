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

    [Header("Animation")]
    /// <summary>AnimationClip, der für diesen Move abgespielt wird.</summary>
    public AnimationClip animationClip;

    /// <summary>Abspielgeschwindigkeit der Animation (1.0 = normal).</summary>
    [Range(0.1f, 3f)]
    public float animationSpeed = 1f;

    /// <summary>Wie lange die Hitbox dieses Moves aktiv bleibt (in Sekunden).</summary>
    public float hitboxActiveTime = 0.2f;
}
