using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class PlayerInputController : NetworkBehaviour
{
    [SyncVar]
    private Vector2 _faceDirection = Vector2.down;
    private InputAction moveInputAction;

    private void Awake()
    {
        if (isClient)
            moveInputAction = InputSystem.actions.FindAction("move");
    }

    void Update()
    {
        if (isOwned)
        {
            Vector2 moveInput = moveInputAction.ReadValue<Vector2>();
            if (moveInput.magnitude > 0)
            {
                CmdMovePlayer(moveInput);
            }
        }
    }


    [Command]
    private void CmdMovePlayer(Vector2 input)
    {
        if (input.magnitude > 0)
        {
            _faceDirection = input.normalized;
            Vector2 newPosition = transform.position + ((Vector3)(input) * GetComponent<Player>().MovementSpeed * Time.deltaTime);
            transform.position = newPosition;
            UpdatePosition(newPosition);
        }
    }


    [ClientRpc]
    private void UpdatePosition(Vector2 position)
    {
        transform.position = position;
    }

    public Vector2 FaceDirection()
    {
        return _faceDirection;
    }
}
