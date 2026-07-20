using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerInputController : NetworkBehaviour
{
    [SyncVar]
    private Vector2 _faceDirection = Vector2.down;
    private Vector2 moveInput;

    void Update()
    {
        if (true || isOwned)
        {
            //Debug.Log("in update, isOwned: " + isOwned);
            Debug.Log(InputSystem.actions.FindAction("move").ReadValue<Vector2>());
            CmdMovePlayer(
               InputSystem.actions.FindAction("move").ReadValue<Vector2>()
            );
            if (isServer)
            {
                /*transform.position +=
                    (Vector3)moveInput *
                    GetComponent<Player>().MovementSpeed *
                    Time.deltaTime;
                */
            }

        }
    }


    [Command]
    private void CmdMovePlayer(Vector2 input)
    {
        Debug.Log("CmdMovePlayer " +  input + ", " + GetComponent<Player>().MovementSpeed + ", " + Time.deltaTime);
        moveInput = input;
        if(moveInput.magnitude > 0)
        {
            _faceDirection = moveInput.normalized; 
        }
       
        Vector2 newPosition = transform.position + ((Vector3)(input) * GetComponent<Player>().MovementSpeed * Time.deltaTime);
        Debug.Log("newPosition: " + newPosition);
        UpdatePosition(newPosition);
    }


    [ClientRpc]
    private void UpdatePosition(Vector2 position)
    {
        Debug.Log("UpdatePosition to " + position);
        transform.position = position;
    }

    public Vector2 FaceDirection()
    {
        return _faceDirection;
    }
}
