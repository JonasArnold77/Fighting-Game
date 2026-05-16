using UnityEngine;

public class FightInputManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatManager combatManager;

    [Header("Moves")]
    [SerializeField] private MoveData leftHandMove;
    [SerializeField] private MoveData rightHandMove;
    [SerializeField] private MoveData leftLegMove;
    [SerializeField] private MoveData rightLegMove;

    [Header("Attack Keys")]
    [SerializeField] private KeyCode leftHandKey  = KeyCode.J;
    [SerializeField] private KeyCode rightHandKey = KeyCode.L;
    [SerializeField] private KeyCode leftLegKey   = KeyCode.K;
    [SerializeField] private KeyCode rightLegKey  = KeyCode.I;

    [Header("Block")]
    [SerializeField] private KeyCode blockKey = KeyCode.Space;

    private void Update()
    {
        HandleAttackInput();
        HandleBlockInput();
    }

    private void HandleAttackInput()
    {
        if (combatManager == null)
            return;

        if (Input.GetKeyDown(leftHandKey) && leftHandMove != null)
            combatManager.ExecutePlayerAttack(leftHandMove);

        if (Input.GetKeyDown(rightHandKey) && rightHandMove != null)
            combatManager.ExecutePlayerAttack(rightHandMove);

        if (Input.GetKeyDown(leftLegKey) && leftLegMove != null)
            combatManager.ExecutePlayerAttack(leftLegMove);

        if (Input.GetKeyDown(rightLegKey) && rightLegMove != null)
            combatManager.ExecutePlayerAttack(rightLegMove);
    }

    private void HandleBlockInput()
    {
        if (combatManager == null)
            return;

        if (Input.GetKeyDown(blockKey))
            combatManager.StartPlayerBlock();

        if (Input.GetKeyUp(blockKey))
            combatManager.StopPlayerBlock();
    }
}
