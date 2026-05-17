using UnityEngine;

/// <summary>
/// Steuert Inverse Kinematics für Angriffs-Endpositionen (z.B. Hand/Fuß trifft Ziel).
/// Muss auf demselben GameObject wie der Animator sitzen, damit
/// OnAnimatorIK vom Animator-System aufgerufen wird.
///
/// Verwendung:
///   EnableIK(AvatarIKGoal.LeftHand, target)   → IK blendet sanft ein
///   DisableIK()                                → IK blendet sanft aus
/// </summary>
[RequireComponent(typeof(Animator))]
public class AttackIKController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Tooltip("Geschwindigkeit mit der die Hand zum Ziel gezogen wird (Weight-Lerp pro Sekunde).\n" +
             "Niedrig (1–5) = sanft/langsam. Hoch (15–30) = schnell/hart.")]
    [SerializeField, Range(1f, 30f)] private float weightLerpSpeed = 15f;

    private AvatarIKGoal activeGoal;
    private Transform    activeTarget;
    private Vector3      activeOffset;
    private bool         ikEnabled;
    private float        currentWeight;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Aktiviert IK auf dem angegebenen Körperglied in Richtung des Ziels.
    /// Kann jederzeit (auch mitten in einer Animation) aufgerufen werden –
    /// das Weight blendet automatisch sanft ein.
    /// </summary>
    /// <param name="limb">Avatar-IK-Goal (z.B. LeftHand, RightFoot).</param>
    /// <param name="target">Transform des Trefferziels (Position + Rotation).</param>
    public void EnableIK(AvatarIKGoal limb, Transform target, float speed = -1f, Vector3 offset = default)
    {
        activeGoal   = limb;
        activeTarget = target;
        activeOffset = offset;
        ikEnabled    = true;

        if (speed >= 0f)
            weightLerpSpeed = speed;
    }

    /// <summary>
    /// Deaktiviert IK. Das Weight blendet sanft auf 0 aus –
    /// kein harter Sprung in die Pose.
    /// </summary>
    public void DisableIK()
    {
        ikEnabled = false;
    }

    // -------------------------------------------------------------------------
    // Unity Animator Callback
    // -------------------------------------------------------------------------

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null)
            return;

        // Ziel-Weight: 1 wenn aktiv und Ziel vorhanden, sonst 0
        float targetWeight = ikEnabled && activeTarget != null ? 1f : 0f;

        if (ikEnabled && currentWeight < 0.05f && targetWeight > 0f)
            Debug.Log($"[IK] OnAnimatorIK läuft – Goal: {activeGoal}, Target: {activeTarget?.name}, Weight beginnt zu steigen");

        // Sanftes Lerpen – nie direkt auf 0 oder 1 setzen
        currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * weightLerpSpeed);

        // Weight immer anwenden (auch während des Ausblendens)
        animator.SetIKPositionWeight(activeGoal, currentWeight);
        animator.SetIKRotationWeight(activeGoal, currentWeight);

        if (activeTarget == null)
            return;

        // Position: Ziel + optionaler Offset
        animator.SetIKPosition(activeGoal, activeTarget.position + activeOffset);

        // Rotation: Glied schaut zum Ziel
        Vector3 directionToTarget = activeTarget.position - transform.position;

        Quaternion ikRotation = directionToTarget.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(directionToTarget)
            : transform.rotation;          // Fallback: aktuelle Figur-Rotation

        animator.SetIKRotation(activeGoal, ikRotation);
    }
}
