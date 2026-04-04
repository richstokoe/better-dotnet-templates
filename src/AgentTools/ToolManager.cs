using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.AI;

namespace RichStokoe.AgentTools;

public class ToolManager
{
    public IList<AITool> GetTools()
    {
        var tools = new List<AITool>();
        var assembly = Assembly.GetExecutingAssembly();

        // Find all types in the RichStokoe.AgentTools namespace and sub-namespaces
        var toolTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null &&
                        t.Namespace.Contains("AgentTools") &&
                        t.IsClass &&
                        t.IsAbstract && // Static classes are abstract and sealed
                        t.IsSealed &&
                        t != typeof(ToolManager));

        foreach (var toolType in toolTypes)
        {
            // Find all public static methods with a Description attribute
            var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<DescriptionAttribute>() != null);

            foreach (var method in methods)
            {
                var tool = AIFunctionFactory.Create(method.CreateDelegate(CreateDelegateType(method)), method.Name);
                tools.Add(tool);
            }
        }

        return tools;
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
