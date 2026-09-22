using Mirror;
using UnityEngine;

public class DamageSource : NetworkBehaviour, IContextBound
{
    private Entity _owner;
    private bool isPlayer;
    public float radius = 1.0f;

    private GameContext _context;

    public void Load(Entity owner)
    {
        _owner = owner;
        isPlayer = _owner is Player;
    }

    [ServerCallback]
    void OnEnable()
    {
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

        if (_context != null && _context.CombatTicks != null)
            _context.CombatTicks.OnTick -= DealDamage;

        _context = context;

        if (_context != null && _context.CombatTicks != null)
            _context.CombatTicks.OnTick += DealDamage;
    }

    [Server]
    private void DealDamage()
    {
        if (_context == null) return;

        var enemies = _context.EnemyGrid.GetNearObjects((Vector2)transform.position, 2f);
        foreach (var enemy in enemies)
        {
            if (Vector2.Distance(enemy.Position, (Vector2)transform.position) <= radius + 0.5f) //0.5f hardocded enemy hitbox
            {
                enemy.ReceiveDamage(new DamageEvent(2));
            }
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, radius);
    }
#endif

}
