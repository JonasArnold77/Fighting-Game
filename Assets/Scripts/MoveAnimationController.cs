using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Verknüpft eine TargetZone mit dem passenden Hit-Reaction-AnimationClip.
/// </summary>
[Serializable]
public struct HitReactionEntry
{
    [Tooltip("Zone, die getroffen wird.")]
    public TargetZone zone;

    [Tooltip("Clip, der bei einem Treffer in dieser Zone abgespielt wird.")]
    public AnimationClip clip;
}

/// <summary>
/// Steuert alle Kampf-Animationen über einen AnimatorOverrideController.
///
/// Warum zwei States pro Typ?
///   CrossFade zu einem bereits aktiven State startet ihn nicht neu.
///   Durch Alternieren zwischen _A und _B wird immer ein echter State-Wechsel
///   erzwungen – der neue Clip spielt garantiert von vorne.
///
/// Animator-Setup (4 States, keine Transitions nötig):
///   GenericAttack_A  – Placeholder-Clip namens "GenericAttack_A"
///   GenericAttack_B  – Placeholder-Clip namens "GenericAttack_B"
///   GenericHit_A     – Placeholder-Clip namens "GenericHit_A"
///   GenericHit_B     – Placeholder-Clip namens "GenericHit_B"
/// </summary>
public class MoveAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Locomotion")]
    [Tooltip("Name des Idle/Walk-States im Animator (z.B. 'Locomotion').")]
    [SerializeField] private string locomotionStateName = "Locomotion";

    [Header("Hit Reactions")]
    [Tooltip("Einen Eintrag pro TargetZone konfigurieren.")]
    [SerializeField] private HitReactionEntry[] hitReactions;

    // State-Hashes
    private static readonly int Hash_Attack_A = Animator.StringToHash("GenericAttack_A");
    private static readonly int Hash_Attack_B = Animator.StringToHash("GenericAttack_B");
    private static readonly int Hash_Hit_A    = Animator.StringToHash("GenericHit_A");
    private static readonly int Hash_Hit_B    = Animator.StringToHash("GenericHit_B");

    // Clip-Namen für Auto-Erkennung
    private const string Name_Attack_A = "GenericAttack_A";
    private const string Name_Attack_B = "GenericAttack_B";
    private const string Name_Hit_A    = "GenericHit_A";
    private const string Name_Hit_B    = "GenericHit_B";

    private const float CrossFadeDuration = 0.05f;
    private const int   AnimatorLayer     = 0;

    private AnimatorOverrideController overrideController;
    private int hashLocomotion;

    private AnimationClip attackPlaceholderA;
    private AnimationClip attackPlaceholderB;
    private AnimationClip hitPlaceholderA;
    private AnimationClip hitPlaceholderB;

    // Welcher State wird als nächstes verwendet
    private bool useAttackA = true;
    private bool useHitA    = true;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;

        hashLocomotion = Animator.StringToHash(locomotionStateName);

        AutoDetectPlaceholders();

        if (attackPlaceholderA == null || attackPlaceholderB == null)
            Debug.LogError("[MoveAnimationController] Attack-Placeholder nicht gefunden. " +
                           "Benenne die Clips im Animator 'GenericAttack_A' und 'GenericAttack_B'.");

        if (hitPlaceholderA == null || hitPlaceholderB == null)
            Debug.LogError("[MoveAnimationController] Hit-Placeholder nicht gefunden. " +
                           "Benenne die Clips im Animator 'GenericHit_A' und 'GenericHit_B'.");
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Spielt den Clip des übergebenen Moves ab normalizedTime 0f ab.
    /// Unterbricht jede laufende Animation, auch denselben Move.
    /// </summary>
    public void PlayMove(MoveData move)
    {
        if (move == null)
        {
            Debug.LogWarning("[MoveAnimationController] PlayMove: move ist null.");
            return;
        }

        if (move.animationClip == null)
        {
            Debug.LogWarning($"[MoveAnimationController] Move '{move.moveName}' hat keinen AnimationClip.");
            return;
        }

        if (attackPlaceholderA == null || attackPlaceholderB == null)
        {
            Debug.LogWarning("[MoveAnimationController] Attack-Placeholder fehlen.");
            return;
        }

        // Beide Slots auf den neuen Clip setzen –
        // egal in welchem State wir landen, der Clip stimmt.
        overrideController[attackPlaceholderA] = move.animationClip;
        overrideController[attackPlaceholderB] = move.animationClip;

        // Zum jeweils anderen State wechseln → erzwingt echten State-Wechsel
        int targetHash = useAttackA ? Hash_Attack_A : Hash_Attack_B;
        useAttackA = !useAttackA;

        animator.speed = move.animationSpeed;
        animator.CrossFade(targetHash, CrossFadeDuration, AnimatorLayer, 0f);
    }

    /// <summary>
    /// Spielt die zur TargetZone passende Hit-Reaction ab normalizedTime 0f ab.
    /// Unterbricht jeden laufenden Move – auch einen anderen Hit-Reaction-Clip.
    /// </summary>
    public void PlayHitReaction(TargetZone zone)
    {
        AnimationClip clip = FindHitReactionClip(zone);

        if (clip == null)
        {
            Debug.LogWarning($"[MoveAnimationController] Kein Hit-Reaction-Clip für Zone '{zone}'.");
            return;
        }

        if (hitPlaceholderA == null || hitPlaceholderB == null)
        {
            Debug.LogWarning("[MoveAnimationController] Hit-Placeholder fehlen.");
            return;
        }

        overrideController[hitPlaceholderA] = clip;
        overrideController[hitPlaceholderB] = clip;

        int targetHash = useHitA ? Hash_Hit_A : Hash_Hit_B;
        useHitA = !useHitA;

        animator.speed = 1f;
        animator.CrossFade(targetHash, CrossFadeDuration, AnimatorLayer, 0f);
    }

    /// <summary>
    /// Kehrt zum Locomotion-State zurück (Idle/Walk-Blend-Tree).
    /// Wird nach einem Angriff oder Treffer aufgerufen.
    /// </summary>
    public void PlayLocomotion()
    {
        animator.speed = 1f;
        animator.CrossFade(hashLocomotion, CrossFadeDuration, AnimatorLayer, 0f);
    }

    // -------------------------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------------------------

    private void AutoDetectPlaceholders()
    {
        var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(pairs);

        foreach (KeyValuePair<AnimationClip, AnimationClip> pair in pairs)
        {
            if (pair.Key == null)
                continue;

            string clipName = pair.Key.name;

            if (clipName == Name_Attack_A) attackPlaceholderA = pair.Key;
            else if (clipName == Name_Attack_B) attackPlaceholderB = pair.Key;
            else if (clipName == Name_Hit_A) hitPlaceholderA = pair.Key;
            else if (clipName == Name_Hit_B) hitPlaceholderB = pair.Key;
        }
    }

    private AnimationClip FindHitReactionClip(TargetZone zone)
    {
        if (hitReactions == null)
            return null;

        foreach (HitReactionEntry entry in hitReactions)
        {
            if (entry.zone == zone)
                return entry.clip;
        }

        return null;
    }
}
