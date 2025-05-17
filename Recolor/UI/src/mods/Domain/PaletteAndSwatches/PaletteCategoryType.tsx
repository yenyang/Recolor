/// <summary>
/// Top level categories for palletes.
/// </summary>
export enum PaletteCategory
{
    /// <summary>
    /// Default will apply to all types of assets.
    /// </summary>
    Any = 0,

    /// <summary>
    /// Limits to buildings.
    /// </summary>
    Buildings = 1,

    /// <summary>
    /// Limits to vehicles.
    /// </summary>
    Vehicles = 2,

    /// <summary>
    /// Limits to props.
    /// </summary>
    Props = 4,

    /// <summary>
    /// Limits to netlanes.
    /// </summary>
    NetLanes = 8,
}