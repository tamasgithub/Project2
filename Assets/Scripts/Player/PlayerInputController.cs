using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : NetworkBehaviour
{
    [SyncVar]
    private Vector2 _faceDirection;
    private Vector2 moveInput;

    void Update()
    {
        if (isOwned)
        {
            CmdMovePlayer(
                InputSystem.actions.FindAction("move").ReadValue<Vector2>()
            );
        }

        if (isServer)
        {
            transform.position += 
                (Vector3)moveInput *
                GetComponent<Player>().MovementSpeed *
                Time.deltaTime;
        }
    }


    [Command]
    private void CmdMovePlayer(Vector2 input)
    {
        moveInput = input;
    }

    public Vector2 FaceDirection()
    {
        return _faceDirection;
    }
}
