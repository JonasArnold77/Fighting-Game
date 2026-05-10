using UnityEngine;

public class FightInputManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatManager combatManager;

    [Header("Attack Keys")]
    [SerializeField] private KeyCode leftHandKey = KeyCode.J;
    [SerializeField] private KeyCode rightHandKey = KeyCode.L;
    [SerializeField] private KeyCode leftLegKey = KeyCode.K;
    [SerializeField] private KeyCode rightLegKey = KeyCode.I;

    [Header("Block")]
    [SerializeField] private KeyCode blockKey = KeyCode.Space;

    private void Update()
    {
        HandleAttackInput();
        HandleBlockInput();
    }

    private void HandleAttackInput()
    {
        if (Input.GetKeyDown(leftHandKey))
        {
            combatManager.ExecutePlayerAttack(BodyPart.LeftHand);
        }

        if (Input.GetKeyDown(rightHandKey))
        {
            combatManager.ExecutePlayerAttack(BodyPart.RightHand);
        }

        if (Input.GetKeyDown(leftLegKey))
        {
            combatManager.ExecutePlayerAttack(BodyPart.LeftLeg);
        }

        if (Input.GetKeyDown(rightLegKey))
        {
            combatManager.ExecutePlayerAttack(BodyPart.RightLeg);
        }
    }

    private void HandleBlockInput()
    {
        if (Input.GetKeyDown(blockKey))
        {
            combatManager.StartPlayerBlock();
        }

        if (Input.GetKeyUp(blockKey))
        {
            combatManager.StopPlayerBlock();
        }
    }
}