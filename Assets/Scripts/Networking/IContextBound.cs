/// <summary>
/// Implemented by components that need to hold on to their lobby's <see cref="GameContext"/>,
/// typically because they subscribe to one of its tick managers.
/// Objects that are moved between scenes after creation (the player) are rebound explicitly
/// by <see cref="GameContext.AttachPlayer"/>; everything else binds itself on enable.
/// </summary>
public interface IContextBound
{
    void BindContext(GameContext context);
}
