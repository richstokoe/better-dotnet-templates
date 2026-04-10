using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace RichStokoe.AgentTools;

public class ToolManager
{
    /// <summary>
    /// Returns all discovered AI tools, optionally filtered by name pattern and/or type.
    /// </summary>
    /// <param name="namePattern">
    /// Glob-style pattern to match against the tool name (supports <c>*</c> and <c>?</c> wildcards).
    /// When <c>null</c>, all tool names are included.
    /// </param>
    /// <param name="typeFilter">
    /// Flags mask to filter by tool type. Only tools whose <see cref="AgentToolAttribute.Type"/>
    /// shares at least one flag with <paramref name="typeFilter"/> are included.
    /// When <see cref="AgentToolTypes.None"/>, no type filtering is applied.
    /// </param>
    public IList<AITool> GetTools(string? namePattern = null, AgentToolTypes typeFilter = AgentToolTypes.None)
    {
        var nameRegex = namePattern is not null ? GlobToRegex(namePattern) : null;

        var tools = new List<AITool>();

        var methods = AssemblyLoadContext.Default.Assemblies
            .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Select(m => (Method: m, Attr: m.GetCustomAttribute<AgentToolAttribute>()))
            .Where(x => x.Attr is not null);

        foreach (var (method, attr) in methods)
        {
            var toolName = attr!.Name ?? method.Name;

            if (nameRegex is not null && !nameRegex.IsMatch(toolName))
                continue;

            if (typeFilter != AgentToolTypes.None && (attr.Type & typeFilter) == AgentToolTypes.None)
                continue;

            var tool = AIFunctionFactory.Create(method.CreateDelegate(CreateDelegateType(method)), toolName);
            tools.Add(tool);
        }

        return tools;
    }

    private static Regex GlobToRegex(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".") + "$";

        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

    private static Type CreateDelegateType(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var typeArgs = parameters.Select(p => p.ParameterType).ToList();
        typeArgs.Add(method.ReturnType);

        return parameters.Length switch
        {
            0 => typeof(Func<>).MakeGenericType(typeArgs.ToArray()),
            1 => typeof(Func<,>).MakeGenericType(typeArgs.ToArray()),
            2 => typeof(Func<,,>).MakeGenericType(typeArgs.ToArray()),
            3 => typeof(Func<,,,>).MakeGenericType(typeArgs.ToArray()),
            4 => typeof(Func<,,,,>).MakeGenericType(typeArgs.ToArray()),
            5 => typeof(Func<,,,,,>).MakeGenericType(typeArgs.ToArray()),
            _ => throw new NotSupportedException($"Methods with {parameters.Length} parameters are not supported")
        };
    }
}
