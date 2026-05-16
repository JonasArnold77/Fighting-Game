using System;
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

    [Header("Attack Timing")]
    [Tooltip("Sekunden nach Animationsstart bis die Hitbox aktiviert wird.")]
    [SerializeField] private float attackImpactDelay = 0.35f;
    [Tooltip("Wie lange die Hitbox im Treffermoment aktiv bleibt.")]
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

    public void ExecutePlayerAttack(BodyPart bodyPart)
    {
        if (player == null || enemy == null)
            return;

        if (player.IsBlocking)
            return;

        if (player.IsAttacking)
        {
            if (allowPlayerAttackCancel && CanCancelAttack(player))
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
            if (allowEnemyAttackCancel && CanCancelAttack(enemy))
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

        playerAttackRoutine = StartCoroutine(
            PerformAttack(player, bodyPart, true)
        );
    }

    private void StartEnemyAttack(BodyPart bodyPart)
    {
        enemyCanAttack = false;

        enemyAttackRoutine = StartCoroutine(
            PerformAttack(enemy, bodyPart, false)
        );
    }

    private void CancelPlayerAttack()
    {
        if (playerAttackRoutine != null)
        {
            StopCoroutine(playerAttackRoutine);
            playerAttackRoutine = null;
        }

        player.ResetAttackTriggers();
        player.StopAttack();
        playerCanAttack = true;

        Debug.Log("Player attack canceled by another attack.");
    }

    private void CancelEnemyAttack()
    {
        if (enemyAttackRoutine != null)
        {
            StopCoroutine(enemyAttackRoutine);
            enemyAttackRoutine = null;
        }

        enemy.ResetAttackTriggers();
        enemy.StopAttack();
        enemyCanAttack = true;

        Debug.Log("Enemy attack canceled by another attack.");
    }

    private bool CanCancelAttack(Fighter attacker)
    {
        if (attacker == null)
            return false;

        if (!attacker.IsInAttackTaggedAnimation())
            return false;

        float animationPercent = attacker.GetCurrentAttackAnimationNormalizedTime();

        return animationPercent >= cancelAfterPercent;
    }

    private IEnumerator PerformAttack(Fighter attacker, BodyPart attackingBodyPart, bool isPlayerAttack)
    {
        attacker.StartAttack();

        yield return attacker.RotateToCubeBeforeAttackAnimation();

        attacker.PlayAttackAnimation(attackingBodyPart);

        // Warten bis zum Treffermoment in der Animation
        yield return new WaitForSeconds(attackImpactDelay);

        // Hitbox aktivieren und auf Kollision warten
        bool hitDetected = false;
        Hurtbox detectedHurtbox = null;

        AttackHitbox hitbox = attacker.ActivateHitboxForBodyPart(attackingBodyPart);

        if (hitbox != null)
        {
            Action<Hurtbox> onHit = (hurtbox) =>
            {
                hitDetected = true;
                detectedHurtbox = hurtbox;
            };

            hitbox.HitDetected += onHit;

            float timer = 0f;
            while (timer < hitboxActiveDuration && !hitDetected)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            hitbox.HitDetected -= onHit;
            attacker.DeactivateAllHitboxes();
        }

        // Schaden anwenden
        if (hitDetected && detectedHurtbox != null)
        {
            float damage = GetDamageForBodyPart(attackingBodyPart);

            if (detectedHurtbox.Owner.IsBlocking)
            {
                damage *= blockDamageMultiplier;
                Debug.Log($"{detectedHurtbox.Owner.name} hat den Angriff geblockt!");
            }

            detectedHurtbox.Owner.TakeDamage(damage);
        }
        else
        {
            Debug.Log($"{attacker.name} hat verfehlt.");
        }

        // Restliche Lock-Zeit abwarten (abzüglich bereits vergangener Zeit)
        float remainingLockTime = Mathf.Max(0f, attackLockDuration - attackImpactDelay - hitboxActiveDuration);
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
            float animationPercent = attacker.GetCurrentAttackAnimationNormalizedTime();

            if (animationPercent >= attackEndPercent)
                yield break;

            timer += Time.deltaTime;

            if (timer >= maximumExtraAnimationWait)
            {
                Debug.LogWarning($"{attacker.name} attack animation wait ended by fallback.");
                yield break;
            }

            yield return null;
        }
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
