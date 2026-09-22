using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public abstract class Projectile : NetworkBehaviour
{
    // default if not overridden by AbilityData in LoadBaseStats
    protected float baseSpeed = 3f;
    protected float maxLifeTime = 10.0f;
    protected int damage = 1;
    protected int pierce = 0;
    protected float size = 1;
    protected float aoeSize = 1; // for explosions etc
    private Vector2 direction;
    protected float LifeTime { get; private set; }

    protected GameContext Context => GameContext.For(this);

    public virtual void LoadStats(int level, AbilityData abilityData, Vector2 direction, Entity _entity)
    {
        LoadBaseStats(level, abilityData);
        this.direction = direction;
        this.size *= _entity.ProjectileSize;
        transform.localScale *= size;
        this.damage += _entity.Damage;
        this.pierce += _entity.Pierce ;
        aoeSize *= _entity.AreaOfEffectSize;

    }

    // Subscribing in OnStartServer rather than OnEnable: OnEnable already runs during
    // Instantiate, before NetworkServer.Spawn, when isServer is still false.
    public override void OnStartServer()
    {
        GetComponent<AreaTrigger>().OnTriggerEnter += OnCollision;
    }

    public override void OnStopServer()
    {
        AreaTrigger trigger = GetComponent<AreaTrigger>();
        if (trigger != null) trigger.OnTriggerEnter -= OnCollision;
    }

    public override void OnStartClient()
    {
        // The server places its objects through GameContext and must never resolve a scene
        // by name: it has one GameScene per lobby. A client only ever has one.
        if (NetworkServer.active) return;
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName("GameScene"));
    }

    protected virtual void Update()
    {
        if (!authority) return;
        Move();
        if (LifeTime >= maxLifeTime)
        {
            OnLifeTimeEnded();
            NetworkServer.Destroy(gameObject);
        }
    }

    protected virtual void Move()
    {
        LifeTime += Time.deltaTime;
        transform.position += (Vector3)direction * baseSpeed * Time.deltaTime;
    }

    protected abstract void LoadBaseStats(int level, AbilityData abilityData);

    // ServerCallback to not log warnings on collision on client side
    [ServerCallback]
    public virtual void OnCollision(ServerEntity collision)
    {

        if (collision is not ServerEnemy enemy) return;
        {
            enemy.ReceiveDamage(new DamageEvent(damage));
            pierce -= 1;
            if (pierce < 0)
            {
                NetworkServer.Destroy(gameObject);
            }
        }
    }

    protected virtual void OnLifeTimeEnded()
    {

    }
}
