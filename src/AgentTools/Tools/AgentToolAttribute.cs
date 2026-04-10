namespace RichStokoe.AgentTools;

/// <summary>
/// Controls whether an agent tool performs read or write operations.
/// Used to filter tools returned by <see cref="ToolManager.GetTools"/>.
/// </summary>
[Flags]
public enum AgentToolTypes
{
    None  = 0,
    Read  = 1,
    Write = 2
}

/// <summary>
/// Marks a static method as an AI agent tool, making it discoverable by <see cref="ToolManager"/>.
/// Methods must also have a <see cref="System.ComponentModel.DescriptionAttribute"/> to provide
/// the tool description exposed to the AI model.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class AgentToolAttribute : Attribute
{
    /// <summary>
    /// Optional display name for the tool. When not set, the method name is used.
    /// This name is also used when filtering with <see cref="ToolManager.GetTools"/>.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Classifies the tool as performing read, write, or both kinds of operations.
    /// Defaults to <see cref="AgentToolTypes.None"/> (unclassified).
    /// </summary>
    public AgentToolTypes Type { get; init; } = AgentToolTypes.None;
}
