using UnityEngine;

/// <summary>
/// Wird auf den Charakter (oder ein Kind-GameObject) gelegt.
/// Immer aktiv – repräsentiert den Körper, der getroffen werden kann.
///
/// SETUP: Dieses GameObject braucht einen Collider (z.B. CapsuleCollider).
/// Owner im Inspector auf den Fighter dieses Charakters setzen.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hurtbox : MonoBehaviour
{
    [Tooltip("Der Fighter, dem diese Hurtbox gehört.")]
    [SerializeField] private Fighter owner;

    public Fighter Owner => owner;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }
}
