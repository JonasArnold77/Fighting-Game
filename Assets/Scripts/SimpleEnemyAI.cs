using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleEnemyAI : MonoBehaviour
{
    [SerializeField] private CombatManager combatManager;

    [Header("Timing")]
    [SerializeField] private float minAttackDelay = 2f;
    [SerializeField] private float maxAttackDelay = 4f;

    private readonly List<MoveData> allMoves = new List<MoveData>();

    /// <summary>
    /// Wird vom MoveSelectionMenu aufgerufen sobald der Spieler seine Auswahl bestätigt.
    /// </summary>
    public void Configure(List<MoveData> selectedMoves)
    {
        allMoves.Clear();

        if (selectedMoves != null)
        {
            foreach (MoveData move in selectedMoves)
                if (move != null) allMoves.Add(move);
        }
    }

    /// <summary>
    /// Startet die KI-Angriffs-Loop. Wird nach Configure() vom Menu aufgerufen.
    /// </summary>
    public void StartFighting()
    {
        if (allMoves.Count == 0)
        {
            Debug.LogWarning("[SimpleEnemyAI] Keine Moves ausgewählt – KI greift nicht an.");
            return;
        }

        StartCoroutine(AILoop());
    }

    private IEnumerator AILoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minAttackDelay, maxAttackDelay));
            combatManager.ExecuteEnemyAttack(allMoves[Random.Range(0, allMoves.Count)]);
        }
    }
}
