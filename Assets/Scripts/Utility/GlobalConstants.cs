using UnityEngine;

public static class GlobalConstants
{

    #region Damage Related
    public const int BLEED_BASE_PERCENTAGE = 8; // Of Max Health
    public const int BLEED_TICK_RATE = 4; // Ticks per second
    #endregion
    #region Tick Rates 
    public const int TRIGGER_CHECK_RATE = 100; // Times Per Second 
    public const int ENEMY_STATE_UPDATE_RATE = 16; // Times Per Second
    #endregion
    #region World
    // Every lobby plays on the same coordinates in its own scene instance,
    // so these bounds describe one lobby's playable area, not the whole server.
    public static readonly Vector2 WORLD_CENTER = Vector2.zero;
    public static readonly Vector2 WORLD_SIZE = Vector2.one * 100;
    public static readonly Vector2Int WORLD_CELLS = Vector2Int.one * 50;
    #endregion
}
