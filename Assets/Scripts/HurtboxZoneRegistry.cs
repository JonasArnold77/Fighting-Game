using System;
using UnityEngine;

/// <summary>
/// Verbindet jede <see cref="TargetZone"/> mit einem konkreten Bone-Transform im Charakter-Rig.
///
/// Zwei Aufgaben:
///   1. IK-Zielpunkt liefern: <see cref="GetZoneTransform"/> gibt dem CombatManager
///      den Bone-Transform, zu dem die angreifende Hand/Fuß via IK gesteuert wird.
///   2. Physik-Hurtbox aufbauen: Beim Start wird auf jeden konfigurierten Bone
///      automatisch ein <see cref="SphereCollider"/> + <see cref="Hurtbox"/> gelegt,
///      damit der AttackHitbox-Trigger dort auslösen kann.
///
/// Setup:
///   1. Komponente auf den Root des Charakters ziehen.
///   2. Owner Fighter zuweisen.
///   3. Pro TargetZone einen Eintrag anlegen und den passenden Bone aus der
///      Rig-Hierarchy zuweisen (z.B. mixamorig:Head für TargetZone.Head).
/// </summary>
public class HurtboxZoneRegistry : MonoBehaviour
{
    [Tooltip("Der Fighter, dem diese Zonen gehören (für Self-Hit-Prüfung in der Hurtbox).")]
    [SerializeField] private Fighter ownerFighter;

    [Tooltip("Eine Zuordnung pro TargetZone. Den jeweiligen Knochen aus dem Rig-Hierarchy zuweisen.")]
    [SerializeField] private ZoneEntry[] zoneEntries;

    [Header("Auto Hurtbox Setup")]
    [Tooltip("Beim Start automatisch SphereCollider + Hurtbox auf jeden Bone legen.\n" +
             "Deaktivieren, wenn du die Hurtboxes manuell platzieren möchtest.")]
    [SerializeField] private bool autoSetupHurtboxes = true;

    [Tooltip("Radius der automatisch erzeugten Hurtbox-SphereCollider pro Zone.")]
    [SerializeField] private float zoneHurtboxRadius = 0.12f;

    // -------------------------------------------------------------------------
    // Unity
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (ownerFighter == null)
            ownerFighter = GetComponentInParent<Fighter>();

        if (ownerFighter == null)
            Debug.LogError($"[HurtboxZoneRegistry] Kein Fighter auf {name} oder Parent gefunden. " +
                           "Owner Fighter im Inspector zuweisen.");

        if (autoSetupHurtboxes)
            SetupZoneHurtboxes();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gibt den Bone-Transform zurück, der der angegebenen Zone entspricht.
    /// Gibt <c>null</c> zurück, wenn keine Zuordnung konfiguriert ist.
    /// </summary>
    /// <param name="zone">Die angegriffene Zone (aus <see cref="MoveData.targetZone"/>).</param>
    public Transform GetZoneTransform(TargetZone zone)
    {
        if (zoneEntries == null)
            return null;

        foreach (ZoneEntry entry in zoneEntries)
        {
            if (entry.zone == zone)
                return entry.boneTransform;
        }

        return null;
    }

    // -------------------------------------------------------------------------
    // Hurtbox Auto-Setup
    // -------------------------------------------------------------------------

    /// <summary>
    /// Legt auf jeden konfigurierten Bone einen <see cref="SphereCollider"/> (Trigger) und
    /// eine <see cref="Hurtbox"/> – sofern dort noch keine vorhanden ist.
    /// </summary>
    private void SetupZoneHurtboxes()
    {
        if (zoneEntries == null)
            return;

        foreach (ZoneEntry entry in zoneEntries)
        {
            if (entry.boneTransform == null)
            {
                Debug.LogWarning($"[HurtboxZoneRegistry] Zone '{entry.zone}' hat keinen Bone zugewiesen.");
                continue;
            }

            // Keine Doppel-Hurtbox anlegen
            if (entry.boneTransform.TryGetComponent<Hurtbox>(out _))
                continue;

            // SphereCollider (Trigger) – kein Rigidbody nötig, AttackHitbox bringt seinen eigenen
            SphereCollider sphere = entry.boneTransform.gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius    = zoneHurtboxRadius;

            // Hurtbox mit Owner verbinden
            Hurtbox hurtbox = entry.boneTransform.gameObject.AddComponent<Hurtbox>();
            hurtbox.SetOwner(ownerFighter);
        }
    }
}

// ---------------------------------------------------------------------------
// Data
// ---------------------------------------------------------------------------

/// <summary>
/// Verknüpft eine <see cref="TargetZone"/> mit einem Rig-Bone.
/// </summary>
[Serializable]
public struct ZoneEntry
{
    [Tooltip("TargetZone, die dieser Knochen repräsentiert (z.B. Head, Torso, ...).")]
    public TargetZone zone;

    [Tooltip("Bone-Transform im Rig (z.B. 'mixamorig:Head'). Wird als IK-Zielpunkt und Hurtbox-Position verwendet.")]
    public Transform boneTransform;
}
