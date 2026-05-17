using System.Collections;
using UnityEngine;

public class Fighter : MonoBehaviour
{
    public bool IsPlayer;

    [Header("Stats")]
    public float Health = 100f;

    [Header("Combat State")]
    public bool IsBlocking;
    public bool IsAttacking;
    public bool IsHit { get; private set; }

    [Header("Hit Stagger")]
    [Tooltip("Wie lange der Charakter nach einem Treffer nicht bewegen kann.")]
    [SerializeField] private float hitStaggerDuration = 0.5f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private MoveAnimationController moveAnimationController;
    [SerializeField] private CharacterController characterController;

    [Header("Attack Hitboxes")]
    [Tooltip("Je eine AttackHitbox pro Körperteil (LeftHand, RightHand, LeftLeg, RightLeg).")]
    [SerializeField] private AttackHitbox[] attackHitboxes;

    [Header("Attack Look At Cube")]
    [SerializeField] private Transform attackLookTarget;
    [SerializeField] private float attackLookRotationSpeed = 720f;
    [SerializeField] private float lookRotationOffsetY = 0f;

    [Header("Before Attack Look At Cube")]
    [SerializeField] private bool rotateToCubeBeforeAttackAnimation = true;
    [SerializeField] private float preAttackRotationSpeed = 1080f;
    [SerializeField] private float preAttackMaxDuration = 0.25f;
    [SerializeField] private float preAttackStopAngle = 2f;

    [Tooltip("Wie lange ab StartAttack zus�tzlich zum Cube gedreht wird.")]
    [SerializeField] private float forceLookAtAfterAttackStartDuration = 0.25f;

    [Tooltip("Bis zu welchem Prozent der Attack-Animation weiter zum Cube gedreht wird. 0.2 = nur die ersten 20%.")]
    [SerializeField, Range(0f, 1f)] private float attackLookAtUntilNormalizedTime = 0.2f;

    [Header("After Attack Look At")]
    [Tooltip("Wenn leer, wird automatisch Attack Look Target benutzt.")]
    [SerializeField] private Transform postAttackLookTarget;

    [SerializeField] private float postAttackRotationSpeed = 360f;
    [SerializeField] private float postAttackStopAngle = 1f;

    [Header("Attack Animator Tag")]
    [SerializeField] private int attackAnimatorLayer = 0;
    [SerializeField] private string attackStateTag = "Attack";

    private bool wasInAttackTaggedState;
    private bool isPostAttackRotating;
    private float attackStartTime;
    private Coroutine hitStaggerCoroutine;

    public bool CanMove => !IsAttacking && !IsBlocking && !IsHit;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (moveAnimationController == null)
            moveAnimationController = GetComponent<MoveAnimationController>();

        if (moveAnimationController == null)
            Debug.LogWarning($"[Fighter] Kein MoveAnimationController auf {name} gefunden – Angriffsanimationen werden nicht abgespielt.");

        if (attackHitboxes != null)
        {
            foreach (AttackHitbox hitbox in attackHitboxes)
            {
                if (hitbox != null)
                    hitbox.Owner = this;
            }
        }
    }

    private void LateUpdate()
    {
        HandleAttackLookAt();
        HandlePostAttackRotation();
    }

    public void SetMoveSpeed(float speed)
    {
        if (animator == null)
            return;

        if (IsAttacking || IsBlocking)
        {
            animator.SetFloat("MoveSpeed", 0f);
            return;
        }

        animator.SetFloat("MoveSpeed", speed);
    }

    public void StartAttack()
    {
        IsAttacking = true;
        isPostAttackRotating = false;
        attackStartTime = Time.time;

        SetMoveSpeed(0f);

        if (animator != null)
            animator.applyRootMotion = false;
    }

    /// <summary>
    /// Spielt einen Move über den MoveAnimationController ab.
    /// </summary>
    /// <param name="move">MoveData mit AnimationClip und Körperteil-Infos.</param>
    /// <param name="ikTarget">
    /// Optionaler IK-Zielpunkt – typischerweise ein Bone-Transform des Gegners
    /// (aus <see cref="HurtboxZoneRegistry"/>). null = kein IK.
    /// </param>
    public void PlayMove(MoveData move, Transform ikTarget = null)
    {
        if (moveAnimationController == null)
        {
            Debug.LogWarning($"[Fighter] PlayMove auf {name} – kein MoveAnimationController vorhanden.");
            return;
        }

        moveAnimationController.PlayMove(move, ikTarget);
    }

    public void StopAttack()
    {
        IsAttacking = false;
        DeactivateAllHitboxes();
        SetMoveSpeed(0f);
        moveAnimationController?.PlayLocomotion();

        if (animator != null)
            animator.applyRootMotion = true;
    }

    /// <summary>
    /// Startet einen Attack-Step: bewegt den Fighter glatt auf <paramref name="targetDistance"/>
    /// Meter Abstand zu <paramref name="targetPosition"/>. Läuft parallel zur Angriffsanimation.
    /// </summary>
    /// <summary>
    /// Lerpt den Fighter auf genau <paramref name="targetDistance"/> Meter Abstand zum Ziel.
    /// Zielposition wird einmal am Anfang berechnet (Gegner-Bewegung danach ignoriert).
    /// Per yield return aufrufen — blockiert bis der Step abgeschlossen ist.
    /// </summary>
    public IEnumerator StepToDistance(Transform target, float targetDistance, float speed)
    {
        if (target == null)
            yield break;

        // Einmalige Berechnung — Gegner-Bewegung danach irrelevant
        Vector3 toTarget  = target.position - transform.position;
        toTarget.y        = 0f;
        float currentDist = toTarget.magnitude;
        float diff        = currentDist - targetDistance;

        if (Mathf.Abs(diff) < 0.02f)
            yield break;

        Vector3 stepTarget = transform.position + toTarget.normalized * diff;
        stepTarget.y       = transform.position.y;

        while (true)
        {
            Vector3 remaining = stepTarget - transform.position;
            remaining.y       = 0f;

            if (remaining.magnitude < 0.02f)
                yield break;

            float   step = Mathf.Min(speed * Time.deltaTime, remaining.magnitude);
            Vector3 move = remaining.normalized * step;

            if (characterController != null)
                characterController.Move(move);
            else
                transform.position += move;

            yield return null;
        }
    }

    /// <summary>
    /// Aktiviert die Hitbox für das angegebene Körperteil und gibt sie zurück.
    /// Gibt null zurück wenn keine passende Hitbox konfiguriert ist.
    /// </summary>
    public AttackHitbox ActivateHitboxForBodyPart(BodyPart bodyPart)
    {
        if (attackHitboxes == null)
            return null;

        foreach (AttackHitbox hitbox in attackHitboxes)
        {
            if (hitbox != null && hitbox.BodyPart == bodyPart)
            {
                hitbox.SetActive(true);
                return hitbox;
            }
        }

        Debug.LogWarning($"[Fighter] Keine Hitbox für {bodyPart} auf {name} gefunden.");
        return null;
    }

    /// <summary>Deaktiviert alle Hitboxen sofort.</summary>
    public void DeactivateAllHitboxes()
    {
        if (attackHitboxes == null)
            return;

        foreach (AttackHitbox hitbox in attackHitboxes)
        {
            if (hitbox != null)
                hitbox.SetActive(false);
        }
    }

    public IEnumerator RotateToCubeBeforeAttackAnimation()
    {
        if (!rotateToCubeBeforeAttackAnimation)
            yield break;

        if (attackLookTarget == null)
            yield break;

        float timer = 0f;

        while (timer < preAttackMaxDuration)
        {
            bool reachedTarget = RotateTowardsTarget(
                attackLookTarget,
                preAttackRotationSpeed,
                preAttackStopAngle
            );

            if (reachedTarget)
                yield break;

            timer += Time.deltaTime;
            yield return null;
        }
    }

    public void PlayAttackAnimation(BodyPart bodyPart)
    {
        if (animator == null)
            return;

        ResetAttackTriggers();

        switch (bodyPart)
        {
            case BodyPart.L_Hand:
                animator.SetTrigger("LeftPunch");
                break;

            case BodyPart.R_Hand:
                animator.SetTrigger("RightPunch");
                break;

            case BodyPart.L_Leg:
                animator.SetTrigger("LeftKick");
                break;

            case BodyPart.R_Leg:
                animator.SetTrigger("RightKick");
                break;
        }
    }

    public void ResetAttackTriggers()
    {
        if (animator == null)
            return;

        animator.ResetTrigger("LeftPunch");
        animator.ResetTrigger("RightPunch");
        animator.ResetTrigger("LeftKick");
        animator.ResetTrigger("RightKick");
    }

    public bool IsInAttackTaggedAnimation()
    {
        if (animator == null)
            return false;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(attackAnimatorLayer);

        if (currentState.IsTag(attackStateTag))
            return true;

        if (animator.IsInTransition(attackAnimatorLayer))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(attackAnimatorLayer);

            if (nextState.IsTag(attackStateTag))
                return true;
        }

        return false;
    }

    public float GetCurrentAttackAnimationNormalizedTime()
    {
        if (animator == null)
            return 0f;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(attackAnimatorLayer);

        if (currentState.IsTag(attackStateTag))
        {
            return Mathf.Clamp01(currentState.normalizedTime);
        }

        if (animator.IsInTransition(attackAnimatorLayer))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(attackAnimatorLayer);

            if (nextState.IsTag(attackStateTag))
            {
                return Mathf.Clamp01(nextState.normalizedTime);
            }
        }

        return 0f;
    }

    public void StartBlock()
    {
        if (IsAttacking)
            return;

        IsBlocking = true;
        SetMoveSpeed(0f);

        if (animator != null)
        {
            animator.SetTrigger("Block");
        }
    }

    public void StopBlock()
    {
        IsBlocking = false;
    }

    /// <param name="hitZone">Getroffene Zone – steuert welche Hit-Animation abgespielt wird.</param>
    public void TakeDamage(float amount, TargetZone hitZone = TargetZone.Torso)
    {
        Health -= amount;
        Health = Mathf.Max(Health, 0f);

        GetComponentInChildren<HitZoneFlash>()?.Flash(hitZone);
        moveAnimationController?.PlayHitReaction(hitZone);

        if (hitStaggerCoroutine != null)
            StopCoroutine(hitStaggerCoroutine);

        hitStaggerCoroutine = StartCoroutine(HitStagger());

        Debug.Log($"{name} took {amount} damage. Health: {Health}");
    }

    private IEnumerator HitStagger()
    {
        IsHit = true;
        SetMoveSpeed(0f);

        // Mindest-Stagger-Zeit abwarten
        yield return new WaitForSeconds(hitStaggerDuration);

        // Dann warten bis die Hit-Animation wirklich fertig ist
        if (moveAnimationController != null)
        {
            while (moveAnimationController.IsPlayingHitAnimation())
                yield return null;
        }

        IsHit = false;
        moveAnimationController?.PlayLocomotion();
        hitStaggerCoroutine = null;
    }

    private void HandleAttackLookAt()
    {
        if (animator == null)
            return;

        bool isInAttackTaggedState = IsInAttackTaggedAnimation();

        if (isInAttackTaggedState)
        {
            wasInAttackTaggedState = true;
            isPostAttackRotating = false;

            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(attackAnimatorLayer);

            if (currentState.IsTag(attackStateTag))
            {
                if (currentState.normalizedTime <= attackLookAtUntilNormalizedTime)
                {
                    RotateTowardsTarget(
                        attackLookTarget,
                        attackLookRotationSpeed,
                        postAttackStopAngle
                    );
                }
            }

            return;
        }

        if (IsAttacking)
        {
            float timeSinceAttackStart = Time.time - attackStartTime;

            if (timeSinceAttackStart <= forceLookAtAfterAttackStartDuration)
            {
                RotateTowardsTarget(
                    attackLookTarget,
                    attackLookRotationSpeed,
                    postAttackStopAngle
                );
            }
        }

        if (wasInAttackTaggedState)
        {
            wasInAttackTaggedState = false;
            isPostAttackRotating = true;
        }
    }

    private void HandlePostAttackRotation()
    {
        if (!isPostAttackRotating)
            return;

        Transform target = postAttackLookTarget != null ? postAttackLookTarget : attackLookTarget;

        bool reachedTargetRotation = RotateTowardsTarget(
            target,
            postAttackRotationSpeed,
            postAttackStopAngle
        );

        if (reachedTargetRotation)
        {
            isPostAttackRotating = false;
        }
    }

    private bool RotateTowardsTarget(Transform target, float rotationSpeed, float stopAngle)
    {
        if (target == null)
            return true;

        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude < 0.001f)
            return true;

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        targetRotation *= Quaternion.Euler(0f, lookRotationOffsetY, 0f);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        float angle = Quaternion.Angle(transform.rotation, targetRotation);
        return angle <= stopAngle;
    }
}