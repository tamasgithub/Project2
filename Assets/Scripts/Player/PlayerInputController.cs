using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : NetworkBehaviour
{
    public Vector2 FaceDirection { get; private set; } = Vector2.down;
    private InputAction moveAction;
    private Vector2 moveInput;
    private Entity player;


    void Start()
    {
        moveAction = InputSystem.actions.FindAction("move");
        player = GetComponent<Player>();
    }

   
    void Update()
    {
        ReadPlayerInput();
    }

    [Client]
    private void ReadPlayerInput()
    {
        if (!isOwned) return;
        if (!moveAction.IsPressed()) return;
        // if(!hasAuthority)
        moveInput = moveAction.ReadValue<Vector2>();
        
        
        CmdMovePlayer(moveInput);
    }

    [Command]
    private void CmdMovePlayer(Vector2 input)
    {

        //Update Facedirection
        FaceDirection = input;
        RcpMovePlayer( input);
    }

    [ClientRpc]
    private void RcpMovePlayer(Vector2 input)
    {
        transform.position += (Vector3) input * Time.deltaTime * player.MovementSpeed;
    }
}
