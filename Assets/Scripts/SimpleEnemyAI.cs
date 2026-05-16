using System.Collections;
using UnityEngine;

public class SimpleEnemyAI : MonoBehaviour
{
    [SerializeField] private CombatManager combatManager;

    [Header("Moves")]
    [Tooltip("Der Gegner wählt zufällig aus dieser Liste.")]
    [SerializeField] private MoveData[] availableMoves;

    [Header("Timing")]
    [SerializeField] private float minAttackDelay = 2f;
    [SerializeField] private float maxAttackDelay = 4f;

    private void Start()
    {
        if (availableMoves == null || availableMoves.Length == 0)
        {
            Debug.LogWarning("[SimpleEnemyAI] Keine Moves konfiguriert – KI greift nicht an.");
            return;
        }

        StartCoroutine(AILoop());
    }

    private IEnumerator AILoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minAttackDelay, maxAttackDelay));

            MoveData move = availableMoves[Random.Range(0, availableMoves.Length)];

            if (move != null)
                combatManager.ExecuteEnemyAttack(move);
        }
    }
}
