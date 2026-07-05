using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : NetworkBehaviour
{
    [SyncVar(hook = nameof(FaceDirectionChanged))]
    public Vector2 FaceDirection = Vector2.down;
    private InputAction moveAction;
    [SyncVar(hook = nameof(MoveInputChanged))]
    private Vector2 moveInput;
    private Entity player;
    [SyncVar]
    public float velocity;
    [SyncVar]
    private Vector3 lastPos;

    public event Action<Vector2> onFaceDirectionChanged;
    public event Action<Vector2, float> onMoveInputChanged;
    void Start()
    {
        moveAction = InputSystem.actions.FindAction("move");
        player = GetComponent<Player>();
        lastPos = transform.position;
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
        velocity = 0;
        transform.position += (Vector3)moveInput * player.MovementSpeed * Time.deltaTime;
        velocity = moveInput.magnitude * player.MovementSpeed;

        Debug.Log(velocity);
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

    #region  Hooks
    public void FaceDirectionChanged(Vector2 oldVec, Vector2 newVec)
    {
        onFaceDirectionChanged?.Invoke(newVec);
    }
    public void MoveInputChanged(Vector2 oldVec, Vector2 newVec)
    {
        onMoveInputChanged?.Invoke(newVec, velocity);
    }
    #endregion

}
