using System;
using UnityEngine;

/// <summary>
/// Wird auf einen Knochen des Angreifers gelegt (z.B. LeftHand, RightFoot).
/// Ist standardmäßig deaktiviert. Der CombatManager aktiviert ihn kurz
/// im Treffermoment. Bei Kollision mit einer Hurtbox wird HitDetected gefeuert.
///
/// SETUP: Dieses GameObject braucht einen Collider (z.B. SphereCollider)
/// und einen kinematischen Rigidbody (wird automatisch angelegt).
/// </summary>
[RequireComponent(typeof(Collider))]
public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private BodyPart bodyPart;

    public BodyPart BodyPart => bodyPart;

    /// <summary>Wird vom Fighter beim Start gesetzt – verhindert Self-Hits.</summary>
    public Fighter Owner { get; set; }

    /// <summary>Feuert, wenn die Hitbox eine Hurtbox eines anderen Fighters trifft.</summary>
    public event Action<Hurtbox> HitDetected;

    private Collider hitboxCollider;
    private bool isActive;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;

        // Kinematischer Rigidbody wird benötigt damit OnTriggerEnter
        // auch ohne Rigidbody auf der Hurtbox-Seite zuverlässig feuert.
        if (!TryGetComponent<Rigidbody>(out _))
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        SetActive(false);
    }

    /// <summary>Hitbox ein- oder ausschalten.</summary>
    public void SetActive(bool active)
    {
        isActive = active;
        if (hitboxCollider != null)
            hitboxCollider.enabled = active;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive)
            return;

        if (!other.TryGetComponent<Hurtbox>(out Hurtbox hurtbox))
            return;

        // Sich selbst nicht treffen
        if (hurtbox.Owner == Owner)
            return;

        // Sofort deaktivieren → nur ein Treffer pro Schwung
        SetActive(false);

        HitDetected?.Invoke(hurtbox);
    }
}
