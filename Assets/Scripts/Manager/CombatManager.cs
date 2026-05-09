using System.Collections;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private Fighter player;
    [SerializeField] private Fighter enemy;

    [Header("Combat Values")]
    [SerializeField] private float punchDamage = 10f;
    [SerializeField] private float kickDamage = 15f;
    [SerializeField] private float attackImpactDelay = 0.4f;
    [SerializeField] private float blockDamageMultiplier = 0.25f;

    public void ExecutePlayerAttack(BodyPart bodyPart)
    {
        StartCoroutine(PerformAttack(player, enemy, bodyPart));
    }

    public void ExecuteEnemyAttack(BodyPart bodyPart)
    {
        StartCoroutine(PerformAttack(enemy, player, bodyPart));
    }

    public void StartPlayerBlock()
    {
        player.StartBlock();
    }

    public void StopPlayerBlock()
    {
        player.StopBlock();
    }

    private IEnumerator PerformAttack(Fighter attacker, Fighter target, BodyPart attackingBodyPart)
    {
        attacker.PlayAttackAnimation(attackingBodyPart);

        yield return new WaitForSeconds(attackImpactDelay);

        float damage = GetDamageForBodyPart(attackingBodyPart);

        if (target.IsBlocking)
        {
            damage *= blockDamageMultiplier;
            Debug.Log($"{target.name} blocked the attack!");
        }

        target.TakeDamage(damage);
    }

    private float GetDamageForBodyPart(BodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case BodyPart.LeftHand:
            case BodyPart.RightHand:
                return punchDamage;

            case BodyPart.LeftLeg:
            case BodyPart.RightLeg:
                return kickDamage;

            default:
                return 5f;
        }
    }
}