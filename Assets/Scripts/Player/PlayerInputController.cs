using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : NetworkBehaviour
{
    [SyncVar]
    private Vector2 _faceDirection = Vector2.down;
    private InputAction moveInputAction;

    // Server side: the input the owner last reported. Movement is integrated from this once
    // per server frame. Previously the client sent one Command per client frame and the server
    // applied each with its own Time.deltaTime, which made the actual speed
    // movementSpeed * (clientFps / serverFps) -- so every player ran at a different speed.
    private Vector2 _moveInput;

    // Client side: last input actually sent, so a held key costs one Command, not one per frame.
    private Vector2 _lastSentInput;
    private const float InputChangeThreshold = 0.01f;

    private void Awake()
    {
        moveInputAction = InputSystem.actions.FindAction("move");
    }

    public override void OnStartServer()
    {
        // The component ships disabled on the prefab so a client cannot steer before the game
        // starts. The server, however, has to tick movement for this player from now on.
        enabled = true;
    }

    private void OnDisable()
    {
        // Losing control while a key is held would otherwise leave the server integrating
        // stale input forever.
        if (isOwned && NetworkClient.active && NetworkClient.ready)
        {
            _lastSentInput = Vector2.zero;
            CmdSetMoveInput(Vector2.zero);
        }
    }

    void Update()
    {
        if (isOwned)
        {
            SendInput();
        }

        if (NetworkServer.active)
        {
            ServerMove(Time.deltaTime);
        }
    }

    [Client]
    private void SendInput()
    {
        // ClampMagnitude rather than normalize: an analog stick keeps its partial deflection,
        // but diagonal keyboard input (length sqrt(2)) no longer moves 41% faster.
        Vector2 moveInput = Vector2.ClampMagnitude(moveInputAction.ReadValue<Vector2>(), 1f);

        if ((moveInput - _lastSentInput).sqrMagnitude < InputChangeThreshold * InputChangeThreshold) return;

        _lastSentInput = moveInput;
        CmdSetMoveInput(moveInput);
    }

    [Command]
    private void CmdSetMoveInput(Vector2 input)
    {
        // Clamp again: the value comes from the client and decides how fast it may travel.
        _moveInput = Vector2.ClampMagnitude(input, 1f);

        if (_moveInput.sqrMagnitude > 0f)
        {
            _faceDirection = _moveInput.normalized;
        }
    }

    [Server]
    private void ServerMove(float deltaTime)
    {
        if (_moveInput.sqrMagnitude <= 0f) return;

        Vector2 newPosition = (Vector2)transform.position
            + _moveInput * GetComponent<Player>().MovementSpeed * deltaTime;
        transform.position = newPosition;
        UpdatePosition(newPosition);
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
