using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : NetworkBehaviour
{
    private InputAction moveAction;
    private Vector2 moveInput;
    void Start()
    {
        moveAction = InputSystem.actions.FindAction("move");
    }

   
    void Update()
    {
        ReadPlayerInput();
    }

    [Client]
    private void ReadPlayerInput()
    {
        // if(!hasAuthority)
        moveInput = moveAction.ReadValue<Vector2>();
        if(moveInput.magnitude <= 0.1f) return;
        CmdMovePlayer();


    }

    [Command]
    private void CmdMovePlayer()
    {
        RcpMovePlayer();
    }

    [ClientRpc]
    private void RcpMovePlayer()
    {
        transform.position += (Vector3) moveInput * Time.deltaTime;
    }
}
