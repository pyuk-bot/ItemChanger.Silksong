using ItemChanger.Enums;
using ItemChanger.Events.Args;
using ItemChanger.Locations;
using ItemChanger.Placements;
using ItemChanger.Tags;
using ItemChanger.Tags.Constraints;
using UnityEngine.SceneManagement;

namespace ItemChanger.Silksong.Tags;

/// <summary>
/// Tag which raises a FSM event when a location's item is given.
/// </summary>
[LocationTag]
public class SendEventToRegisterOnGiveTag : Tag, IActionOnContainerReplaceTag
{    
    /// <summary>
    /// Name of the event to invoke.
    /// </summary>
    public required string Event { get; init; }
    
    /// <summary>
    /// Only send the event to register if the location was replaced with a container.
    /// </summary>
    public bool OnlyIfContainerReplaced { get; init; } = false;

    private bool _wasReplaced = false;

    protected override void DoLoad(TaggableObject parent)
    {
        Placement? placement = (parent as Location)?.Placement;
        if (placement != null)
        {
            placement.OnVisited += OnVisited;
        }
        else
        {
            LogInfo($"Not a valid location with placement: {parent}");
        }

        ItemChangerHost.Singleton.GameEvents.BeforeNextSceneLoaded += ResetState;
    }

    protected override void DoUnload(TaggableObject parent)
    {
        Placement? placement = (parent as Location)?.Placement;
        if (placement != null)
        {
            placement.OnVisited -= OnVisited;
        }

        ItemChangerHost.Singleton.GameEvents.BeforeNextSceneLoaded -= ResetState;
    }
    
    private void ResetState(Events.Args.BeforeSceneLoadedEventArgs obj)
    {
        _wasReplaced = false;
    }

    public void OnReplace(Scene scene, GameObject newContainer)
    {
        _wasReplaced = true;
    }

    private void OnVisited(PlacementVisitedEventArgs args)
    {
        if (OnlyIfContainerReplaced && !_wasReplaced)
            return;
        if ((args.ProposedNewFlags & VisitState.ObtainedAnyItem) == 0)
            return;
        EventRegister.SendEvent(Event);
    }
}