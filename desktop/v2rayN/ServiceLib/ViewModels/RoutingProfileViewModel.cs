namespace ServiceLib.ViewModels;

/// <summary>A routing profile in the routing chip sheet of the main screen.</summary>
public partial class RoutingProfileViewModel : ReactiveObject
{
    public string Id { get; }

    /// <summary>Localised name (built-ins) or the profile remarks (custom profiles).</summary>
    public string Name { get; }

    /// <summary>Localised description of a built-in profile ("" for custom profiles).</summary>
    public string Description { get; }

    public bool HasDescription => Description.IsNotEmpty();

    public bool IsBuiltin { get; }

    [Reactive] public partial bool IsActive { get; set; }

    public RoutingProfileViewModel(RoutingItem item, bool isActive)
    {
        Id = item.Id;
        Name = RoutingPresets.GetDisplayName(item);
        Description = RoutingPresets.GetDescription(item);
        IsBuiltin = RoutingPresets.IsBuiltin(item.Id);
        IsActive = isActive;
    }
}
