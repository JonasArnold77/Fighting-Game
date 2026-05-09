using UnityEngine;

public class FightInputManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private LayerMask clickableLayer;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryAttackWithClickedBodyPart();
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Space))
        {
            combatManager.StartPlayerBlock();
        }

        if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.Space))
        {
            combatManager.StopPlayerBlock();
        }
    }

    private void TryAttackWithClickedBodyPart()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickableLayer))
        {
            Debug.Log("Raycast hit: " + hit.collider.name);

            BodyPartClickable clickable = hit.collider.GetComponent<BodyPartClickable>();

            if (clickable == null)
            {
                Debug.Log("Hit object has no BodyPartClickable script.");
                return;
            }

            Debug.Log("Clicked body part: " + clickable.BodyPart);

            if (!clickable.Owner.IsPlayer)
            {
                Debug.Log("Clicked body part does not belong to player.");
                return;
            }

            combatManager.ExecutePlayerAttack(clickable.BodyPart);
        }
        else
        {
            Debug.Log("Raycast hit nothing.");
        }
    }
}