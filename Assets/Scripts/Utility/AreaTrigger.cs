using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class AreaTrigger : NetworkBehaviour, IContextBound
{
    public float radius = 1.0f;
    private float _currentRadius;
    private HashSet<ServerEntity> _inside = new();
    public event Action<ServerEntity> OnTriggerEnter;
    public event Action<ServerEntity> OnTriggerExit;

    private GameContext _context;

    void Start()
    {
        _currentRadius = transform.localScale.magnitude * radius;
    }

    [ServerCallback]
    void OnEnable()
    {
        // Objects spawned through GameContext.Spawn already live in their lobby's scene,
        // so they find their context right away. The player is moved between scenes after
        // spawning and is rebound explicitly by GameContext.AttachPlayer.
        BindContext(GameContext.For(this));
    }

    [ServerCallback]
    void OnDisable()
    {
        BindContext(null);
    }

    public void BindContext(GameContext context)
    {
        if (_context == context) return;

        if (_context != null && _context.TriggerTicks != null)
            _context.TriggerTicks.OnTick -= CheckTrigger;

        _context = context;
        _inside.Clear();

        if (_context != null && _context.TriggerTicks != null)
            _context.TriggerTicks.OnTick += CheckTrigger;
    }

    private void CheckTrigger()
    {
        if (_context == null) return;

        var newlyEntered = new HashSet<ServerEntity>();
        var enemies = _context.EnemyGrid.GetNearObjects((Vector2)transform.position, _currentRadius);
        foreach (var enemy in enemies)
        {
            if (Vector2.Distance(enemy.Position, (Vector2)transform.position) <= _currentRadius + 0.5f) //0.5f hardocded enemy hitbox
            {
                newlyEntered.Add(enemy);
                if (!_inside.Contains(enemy))
                {
                    OnTriggerEnter?.Invoke(enemy);
                }
            }
        }

        foreach (var current in _inside)
        {
            if (!newlyEntered.Contains(current))
            {
                OnTriggerExit?.Invoke(current);
            }
        }

        _inside.Clear();
        _inside.UnionWith(newlyEntered);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, radius * transform.localScale.magnitude);
    }
#endif

}
