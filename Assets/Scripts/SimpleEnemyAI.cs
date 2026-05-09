using System.Collections;
using UnityEngine;

public class SimpleEnemyAI : MonoBehaviour
{
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private float minAttackDelay = 2f;
    [SerializeField] private float maxAttackDelay = 4f;

    private void Start()
    {
        StartCoroutine(AILoop());
    }

    private IEnumerator AILoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minAttackDelay, maxAttackDelay));

            BodyPart randomAttack = GetRandomAttack();
            combatManager.ExecuteEnemyAttack(randomAttack);
        }
    }

    private BodyPart GetRandomAttack()
    {
        int value = Random.Range(0, 4);

        switch (value)
        {
            case 0:
                return BodyPart.LeftHand;

            case 1:
                return BodyPart.RightHand;

            case 2:
                return BodyPart.LeftLeg;

            default:
                return BodyPart.RightLeg;
        }
    }
}