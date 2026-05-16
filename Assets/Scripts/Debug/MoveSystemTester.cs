using UnityEngine;

/// <summary>
/// Temporäres Testscript – auf denselben GameObject wie MoveAnimationController legen.
/// Im Playmode Tasten drücken um PlayMove / PlayHitReaction zu testen.
/// Vor dem Release entfernen.
/// </summary>
public class MoveSystemTester : MonoBehaviour
{
    [Header("Referenz")]
    [SerializeField] private MoveAnimationController animationController;

    [Header("Test-Moves (MoveData Assets)")]
    [SerializeField] private MoveData[] testMoves;

    [Header("Test-HitReaction")]
    [SerializeField] private TargetZone testHitZone = TargetZone.Torso;

    [Header("Tasten")]
    [SerializeField] private KeyCode hitReactionKey = KeyCode.H;

    private void Awake()
    {
        if (animationController == null)
            animationController = GetComponent<MoveAnimationController>();
    }

    private void Update()
    {
        // Moves: Tasten 1–9 spielen testMoves[0–8] ab
        for (int i = 0; i < testMoves.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                Debug.Log($"[Tester] PlayMove: {testMoves[i].moveName}");
                animationController.PlayMove(testMoves[i]);
            }
        }

        // H → HitReaction für die konfigurierte Zone
        if (Input.GetKeyDown(hitReactionKey))
        {
            Debug.Log($"[Tester] PlayHitReaction: {testHitZone}");
            animationController.PlayHitReaction(testHitZone);
        }
    }
}
