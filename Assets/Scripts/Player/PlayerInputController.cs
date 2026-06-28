using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : NetworkBehaviour
{
    [SyncVar] public Vector2 FaceDirection = Vector2.down;
    private InputAction moveAction;
    [SyncVar] private Vector2 moveInput;
    private Entity player;


    void Start()
    {
        moveAction = InputSystem.actions.FindAction("move");
        player = GetComponent<Player>();
    }


    void Update()
    {


        if (isClient)
        {
            ReadPlayerInput();
        }
        if (isServer)
        {
            UpdateMovement();
        }



    }

    private void ReadPlayerInput()
    {
        if (!isOwned) return;

        // if(!hasAuthority)
        moveInput = moveAction.ReadValue<Vector2>();
        if (moveAction.IsPressed())
        {
            CmdUpdateFacedirection(moveInput.normalized);
        }
        CmdMovePlayer(moveInput);
    }
    [ServerCallback]
    private void UpdateMovement()
    {
        transform.position += (Vector3)moveInput * player.MovementSpeed * Time.deltaTime;
    }
    [Command]
    private void CmdMovePlayer(Vector2 input)
    {      //Update Facedirection
        moveInput = input;

    }

    [Command]
    private void CmdUpdateFacedirection(Vector2 input)
    {      //Update Facedirection
        FaceDirection = input;

    }

    // [ClientRpc]
    // private void RcpMovePlayer(Vector2 input)
    // {
    //     transform.position = Vector3.Lerp(transform.position, transform.position + (Vector3)input, Time.deltaTime * player.MovementSpeed);
    // }
}
