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

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Attack Look At Cube")]
    [SerializeField] private Transform attackLookTarget;
    [SerializeField] private float attackLookRotationSpeed = 720f;
    [SerializeField] private float lookRotationOffsetY = 0f;

    [Header("Before Attack Look At Cube")]
    [SerializeField] private bool rotateToCubeBeforeAttackAnimation = true;
    [SerializeField] private float preAttackRotationSpeed = 1080f;
    [SerializeField] private float preAttackMaxDuration = 0.25f;
    [SerializeField] private float preAttackStopAngle = 2f;

    [Tooltip("Wie lange ab StartAttack zusätzlich zum Cube gedreht wird.")]
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

    public bool CanMove => !IsAttacking && !IsBlocking;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
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
    }

    public void StopAttack()
    {
        IsAttacking = false;
        SetMoveSpeed(0f);
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
            case BodyPart.LeftHand:
                animator.SetTrigger("LeftPunch");
                break;

            case BodyPart.RightHand:
                animator.SetTrigger("RightPunch");
                break;

            case BodyPart.LeftLeg:
                animator.SetTrigger("LeftKick");
                break;

            case BodyPart.RightLeg:
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

    public void TakeDamage(float amount)
    {
        Health -= amount;
        Health = Mathf.Max(Health, 0f);

        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        Debug.Log($"{name} took {amount} damage. Health: {Health}");
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