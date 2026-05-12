using System.Collections;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("Fighters")]
    [SerializeField] private Fighter player;
    [SerializeField] private Fighter enemy;

    [Header("Damage")]
    [SerializeField] private float punchDamage = 10f;
    [SerializeField] private float kickDamage = 15f;
    [SerializeField] private float blockDamageMultiplier = 0.25f;

    [Header("Attack Settings")]
    [SerializeField] private float punchRange = 1.4f;
    [SerializeField] private float kickRange = 1.8f;
    [SerializeField] private float attackImpactDelay = 0.35f;
    [SerializeField] private float attackLockDuration = 0.8f;
    [SerializeField] private float attackCooldown = 0.2f;
    [SerializeField] private float attackAngle = 90f;

    [Header("Animation Cancel")]
    [SerializeField, Range(0f, 1f)] private float cancelAfterPercent = 0.7f;
    [SerializeField] private bool allowPlayerAttackCancel = true;
    [SerializeField] private bool allowEnemyAttackCancel = false;

    private bool playerCanAttack = true;
    private bool enemyCanAttack = true;

    private Coroutine playerAttackRoutine;
    private Coroutine enemyAttackRoutine;

    private float playerAttackStartTime;
    private float enemyAttackStartTime;

    public void ExecutePlayerAttack(BodyPart bodyPart)
    {
        if (player == null || enemy == null)
            return;

        if (player.IsBlocking)
            return;

        if (player.IsAttacking)
        {
            if (allowPlayerAttackCancel && CanCancelAttack(playerAttackStartTime))
            {
                CancelPlayerAttack();
                StartPlayerAttack(bodyPart);
            }

            return;
        }

        if (!playerCanAttack)
            return;

        StartPlayerAttack(bodyPart);
    }

    public void ExecuteEnemyAttack(BodyPart bodyPart)
    {
        if (enemy == null || player == null)
            return;

        if (enemy.IsBlocking)
            return;

        if (enemy.IsAttacking)
        {
            if (allowEnemyAttackCancel && CanCancelAttack(enemyAttackStartTime))
            {
                CancelEnemyAttack();
                StartEnemyAttack(bodyPart);
            }

            return;
        }

        if (!enemyCanAttack)
            return;

        StartEnemyAttack(bodyPart);
    }

    public void StartPlayerBlock()
    {
        if (player == null)
            return;

        if (player.IsAttacking)
            return;

        player.StartBlock();
    }

    public void StopPlayerBlock()
    {
        if (player == null)
            return;

        player.StopBlock();
    }

    private void StartPlayerAttack(BodyPart bodyPart)
    {
        playerCanAttack = false;
        playerAttackStartTime = Time.time;

        playerAttackRoutine = StartCoroutine(
            PerformAttack(player, enemy, bodyPart, true)
        );
    }

    private void StartEnemyAttack(BodyPart bodyPart)
    {
        enemyCanAttack = false;
        enemyAttackStartTime = Time.time;

        enemyAttackRoutine = StartCoroutine(
            PerformAttack(enemy, player, bodyPart, false)
        );
    }

    private void CancelPlayerAttack()
    {
        if (playerAttackRoutine != null)
        {
            StopCoroutine(playerAttackRoutine);
            playerAttackRoutine = null;
        }

        player.StopAttack();
        playerCanAttack = true;

        Debug.Log("Player attack canceled.");
    }

    private void CancelEnemyAttack()
    {
        if (enemyAttackRoutine != null)
        {
            StopCoroutine(enemyAttackRoutine);
            enemyAttackRoutine = null;
        }

        enemy.StopAttack();
        enemyCanAttack = true;

        Debug.Log("Enemy attack canceled.");
    }

    private bool CanCancelAttack(float attackStartTime)
    {
        float elapsedTime = Time.time - attackStartTime;
        float currentPercent = elapsedTime / attackLockDuration;

        return currentPercent >= cancelAfterPercent;
    }

    private IEnumerator PerformAttack(
        Fighter attacker,
        Fighter target,
        BodyPart attackingBodyPart,
        bool isPlayerAttack
    )
    {
        attacker.StartAttack();

        // NEU:
        // Erst zum Cube drehen, dann Animation starten.
        yield return attacker.RotateToCubeBeforeAttackAnimation();

        attacker.PlayAttackAnimation(attackingBodyPart);

        yield return new WaitForSeconds(attackImpactDelay);

        if (IsTargetInAttackRange(attacker.transform, target.transform, attackingBodyPart))
        {
            float damage = GetDamageForBodyPart(attackingBodyPart);

            if (target.IsBlocking)
            {
                damage *= blockDamageMultiplier;
                Debug.Log($"{target.name} blocked the attack!");
            }

            target.TakeDamage(damage);
        }
        else
        {
            Debug.Log($"{attacker.name} missed.");
        }

        float remainingLockTime = Mathf.Max(0f, attackLockDuration - attackImpactDelay);

        yield return new WaitForSeconds(remainingLockTime);

        attacker.StopAttack();

        yield return new WaitForSeconds(attackCooldown);

        if (isPlayerAttack)
        {
            playerCanAttack = true;
            playerAttackRoutine = null;
        }
        else
        {
            enemyCanAttack = true;
            enemyAttackRoutine = null;
        }
    }

    private bool IsTargetInAttackRange(Transform attacker, Transform target, BodyPart bodyPart)
    {
        Vector3 directionToTarget = target.position - attacker.position;
        directionToTarget.y = 0f;

        float distance = directionToTarget.magnitude;
        float range = IsKick(bodyPart) ? kickRange : punchRange;

        if (distance > range)
            return false;

        float angleToTarget = Vector3.Angle(attacker.forward, directionToTarget);

        if (angleToTarget > attackAngle * 0.5f)
            return false;

        return true;
    }

    private float GetDamageForBodyPart(BodyPart bodyPart)
    {
        if (IsKick(bodyPart))
            return kickDamage;

        return punchDamage;
    }

    private bool IsKick(BodyPart bodyPart)
    {
        return bodyPart == BodyPart.LeftLeg || bodyPart == BodyPart.RightLeg;
    }
}