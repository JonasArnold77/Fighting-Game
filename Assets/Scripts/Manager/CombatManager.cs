using System;
using System.Collections;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("Fighters")]
    [SerializeField] private Fighter player;
    [SerializeField] private Fighter enemy;

    [Header("Damage")]
    [SerializeField] private float blockDamageMultiplier = 0.25f;

    [Header("Attack Timing")]
    [Tooltip("Sekunden nach Animationsstart bis die Hitbox aktiviert wird.")]
    [SerializeField] private float attackImpactDelay = 0.35f;
    [Tooltip("Fallback-Hitbox-Dauer falls MoveData.hitboxActiveTime = 0.")]
    [SerializeField] private float hitboxActiveDuration = 0.2f;
    [SerializeField] private float attackLockDuration = 0.8f;
    [SerializeField] private float attackCooldown = 0.2f;

    [Header("Attack Animation Protection")]
    [SerializeField, Range(0f, 1f)] private float attackEndPercent = 0.98f;
    [SerializeField] private float maximumExtraAnimationWait = 1.5f;

    [Header("Animation Cancel")]
    [SerializeField, Range(0f, 1f)] private float cancelAfterPercent = 0.9f;
    [SerializeField] private bool allowPlayerAttackCancel = true;
    [SerializeField] private bool allowEnemyAttackCancel = false;

    private bool playerCanAttack = true;
    private bool enemyCanAttack = true;

    private Coroutine playerAttackRoutine;
    private Coroutine enemyAttackRoutine;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public void ExecutePlayerAttack(MoveData move)
    {
        if (move == null || player == null || enemy == null)
            return;

        if (player.IsBlocking)
            return;

        if (player.IsAttacking)
        {
            if (allowPlayerAttackCancel && CanCancelAttack(player))
            {
                CancelPlayerAttack();
                StartPlayerAttack(move);
            }

            return;
        }

        if (!playerCanAttack)
            return;

        StartPlayerAttack(move);
    }

    public void ExecuteEnemyAttack(MoveData move)
    {
        if (move == null || enemy == null || player == null)
            return;

        if (enemy.IsBlocking)
            return;

        if (enemy.IsAttacking)
        {
            if (allowEnemyAttackCancel && CanCancelAttack(enemy))
            {
                CancelEnemyAttack();
                StartEnemyAttack(move);
            }

            return;
        }

        if (!enemyCanAttack)
            return;

        StartEnemyAttack(move);
    }

    public void StartPlayerBlock()
    {
        if (player == null || player.IsAttacking)
            return;

        player.StartBlock();
    }

    public void StopPlayerBlock()
    {
        if (player == null)
            return;

        player.StopBlock();
    }

    // -------------------------------------------------------------------------
    // Private
    // -------------------------------------------------------------------------

    private void StartPlayerAttack(MoveData move)
    {
        playerCanAttack = false;
        playerAttackRoutine = StartCoroutine(PerformAttack(player, move, true));
    }

    private void StartEnemyAttack(MoveData move)
    {
        enemyCanAttack = false;
        enemyAttackRoutine = StartCoroutine(PerformAttack(enemy, move, false));
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

    private bool CanCancelAttack(Fighter attacker)
    {
        if (!attacker.IsInAttackTaggedAnimation())
            return false;

        return attacker.GetCurrentAttackAnimationNormalizedTime() >= cancelAfterPercent;
    }

    private IEnumerator PerformAttack(Fighter attacker, MoveData move, bool isPlayerAttack)
    {
        attacker.StartAttack();

        yield return attacker.RotateToCubeBeforeAttackAnimation();

        // Clip über MoveAnimationController abspielen
        attacker.PlayMove(move);

        yield return new WaitForSeconds(attackImpactDelay);

        // Hitbox aktivieren
        bool hitDetected = false;
        Hurtbox detectedHurtbox = null;

        AttackHitbox hitbox = attacker.ActivateHitboxForBodyPart(move.bodyPart);

        if (hitbox != null)
        {
            Action<Hurtbox> onHit = (hurtbox) =>
            {
                hitDetected = true;
                detectedHurtbox = hurtbox;
            };

            hitbox.HitDetected += onHit;

            float activeTime = move.hitboxActiveTime > 0f ? move.hitboxActiveTime : hitboxActiveDuration;
            float timer = 0f;

            while (timer < activeTime && !hitDetected)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            hitbox.HitDetected -= onHit;
            attacker.DeactivateAllHitboxes();
        }

        // Treffer verarbeiten
        if (hitDetected && detectedHurtbox != null)
        {
            float damage = move.damage;

            if (detectedHurtbox.Owner.IsBlocking)
            {
                damage *= blockDamageMultiplier;
                Debug.Log($"{detectedHurtbox.Owner.name} hat geblockt!");
            }

            detectedHurtbox.Owner.TakeDamage(damage, move.targetZone);
        }
        else
        {
            Debug.Log($"{attacker.name} hat verfehlt.");
        }

        float activeHitboxTime = move.hitboxActiveTime > 0f ? move.hitboxActiveTime : hitboxActiveDuration;
        float remainingLockTime = Mathf.Max(0f, attackLockDuration - attackImpactDelay - activeHitboxTime);
        yield return new WaitForSeconds(remainingLockTime);

        yield return WaitUntilAttackAnimationCanEnd(attacker);

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

    private IEnumerator WaitUntilAttackAnimationCanEnd(Fighter attacker)
    {
        float timer = 0f;

        while (attacker != null && attacker.IsInAttackTaggedAnimation())
        {
            if (attacker.GetCurrentAttackAnimationNormalizedTime() >= attackEndPercent)
                yield break;

            timer += Time.deltaTime;

            if (timer >= maximumExtraAnimationWait)
            {
                Debug.LogWarning($"{attacker.name}: Fallback — Animation-Wait abgebrochen.");
                yield break;
            }

            yield return null;
        }
    }
}
