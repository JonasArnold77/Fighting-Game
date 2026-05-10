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

    public bool CanMove => !IsAttacking && !IsBlocking;

    public void SetMoveSpeed(float speed)
    {
        animator.SetFloat("MoveSpeed", speed);
    }

    public void StartAttack()
    {
        IsAttacking = true;
        SetMoveSpeed(0f);
    }

    public void StopAttack()
    {
        IsAttacking = false;
    }

    public void PlayAttackAnimation(BodyPart bodyPart)
    {
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

    public void StartBlock()
    {
        IsBlocking = true;
        SetMoveSpeed(0f);
        animator.SetTrigger("Block");
    }

    public void StopBlock()
    {
        IsBlocking = false;
    }

    public void TakeDamage(float amount)
    {
        Health -= amount;
        Health = Mathf.Max(Health, 0f);

        animator.SetTrigger("Hit");

        Debug.Log($"{name} took {amount} damage. Health: {Health}");
    }
}