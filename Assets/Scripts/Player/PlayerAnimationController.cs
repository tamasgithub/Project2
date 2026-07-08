using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator _animator;
    private AnimatorOverrideController _anim;
    private Dictionary<string, AnimationClip> _animations = new();
    private PlayerInputController _input;
    private SpriteRenderer _renderer;
    public float flipThreshold = 0.2f;
    public float runSpeedMultiplier = 0.3f;
    private string _state = "Idle";

    void Awake()
    {


        _renderer = GetComponentInChildren<SpriteRenderer>();
        // Value References
        _input = GetComponent<PlayerInputController>();
        // _input.onFaceDirectionChanged +=
        // (dir) => { if (Mathf.Abs(dir.x) < flipThreshold) return; _renderer.flipX = dir.x < 0.0f; };
        // _input.onMoveInputChanged +=
        // (dir, velocity) => { if (velocity > 0.1f) { SetAnimationState("Run"); } else { SetAnimationState("Idle"); } };
    }

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _anim = new AnimatorOverrideController(_animator.runtimeAnimatorController);
        _animator.runtimeAnimatorController = _anim;
        foreach (var clip in _anim.animationClips)
        {
            _animations[clip.name] = clip;

        }
    }
    // void Update()
    // {
    //     if (!NetworkClient.active) return;
    //     if (_input.velocity > 0.2f)
    //     {
    //         SetAnimationState("Run");
    //     }
    //     else
    //     {
    //         SetAnimationState("Idle");
    //     }
    // }

    // public void SetAnimationState(string state)
    // {
    //     // if (!_animations.ContainsKey(state)) Debug.LogWarning($"Tried to play Unknown Animation: {state} ");
    //     if (_state != state)
    //     {
    //         _state = state;
    //         if (_state == "Run")
    //         {
    //             _animator.speed = _input.velocity * runSpeedMultiplier;
    //         }
    //         else
    //         {
    //             _animator.speed = 1;
    //         }
    //         _animator.Play(state);
    //     }
    // }


}


