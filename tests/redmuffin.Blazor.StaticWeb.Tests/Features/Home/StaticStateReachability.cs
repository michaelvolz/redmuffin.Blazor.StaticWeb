using System.Reflection;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Home;

/// <summary>
///     Finds what keeps an object alive. Every static field in the process is a starting
///     point, strong references are followed from there, and a path in the result means
///     static state reaches the object.
/// </summary>
/// <remarks>
///     Reflection over runtime types this walk did not create throws for reasons that say
///     nothing about the walk, so unreadable fields and types are skipped. The root count
///     reaches the caller, so a walk that read nothing fails its test instead of passing
///     quietly.
/// </remarks>
internal static class StaticStateReachability
{
    private const int MaxNodes = 400_000;

    internal readonly record struct WalkResult(string? Path, int StaticRoots, int Visited);

    internal static WalkResult Walk(object target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var queue = new Queue<(object Node, string Path)>();

        foreach (var (name, value) in StaticRoots())
        {
            queue.Enqueue((value, name));
        }

        var roots = queue.Count;
        var nodes = 0;

        while (queue.Count > 0)
        {
            var (node, path) = queue.Dequeue();
            if (!visited.Add(node))
            {
                continue;
            }

            nodes++;
            if (nodes > MaxNodes)
            {
                throw new InvalidOperationException(
                    $"The static state walk visited more than {MaxNodes} objects before it reached {target.GetType().FullName}."
                );
            }

            if (ReferenceEquals(node, target))
            {
                return new WalkResult(path, roots, nodes);
            }

            foreach (var (child, label) in Members(node))
            {
                if (!visited.Contains(child))
                {
                    queue.Enqueue((child, $"{path} -> {label}"));
                }
            }
        }

        return new WalkResult(null, roots, nodes);
    }

    private static IEnumerable<(string Name, object Value)> StaticRoots()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
            }
            catch
            {
                continue;
            }

            foreach (var type in types)
            {
                if (type.IsGenericTypeDefinition)
                {
                    continue;
                }

                FieldInfo[] fields;
                try
                {
                    fields = type.GetFields(
                        BindingFlags.Static
                            | BindingFlags.Public
                            | BindingFlags.NonPublic
                            | BindingFlags.FlattenHierarchy
                    );
                }
                catch
                {
                    continue;
                }

                foreach (var field in fields)
                {
                    if (field.IsLiteral || field.FieldType.IsPointer)
                    {
                        continue;
                    }

                    object? value;
                    try
                    {
                        value = field.GetValue(null);
                    }
                    catch
                    {
                        continue;
                    }

                    if (IsTraversable(value))
                    {
                        yield return ($"static {type.FullName}.{field.Name}", value!);
                    }
                }
            }
        }
    }

    private static IEnumerable<(object Child, string Label)> Members(object node)
    {
        if (node is Array array)
        {
            var length = array.Length;
            for (var index = 0; index < length; index++)
            {
                object? element;
                try
                {
                    element = array.GetValue(index);
                }
                catch
                {
                    break;
                }

                if (IsTraversable(element))
                {
                    yield return (element!, $"[{index}]");
                }
            }

            yield break;
        }

        var type = node.GetType();

        // A weak reference keeps no target, so following one would invent a path. A weak-keyed
        // table is skipped on the same grounds, which puts a page held as a table value beyond
        // this walk.
        if (
            type.FullName is not null
            && (
                type.FullName.StartsWith("System.WeakReference", StringComparison.Ordinal)
                || type.FullName.StartsWith(
                    "System.Runtime.CompilerServices.ConditionalWeakTable",
                    StringComparison.Ordinal
                )
            )
        )
        {
            yield break;
        }

        if (node is Delegate callback)
        {
            if (callback.Target is { } callbackTarget && IsTraversable(callbackTarget))
            {
                yield return (callbackTarget, "Target");
            }

            yield break;
        }

        while (type is not null)
        {
            FieldInfo[] fields;
            try
            {
                fields = type.GetFields(
                    BindingFlags.Instance
                        | BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.DeclaredOnly
                );
            }
            catch
            {
                break;
            }

            foreach (var field in fields)
            {
                if (field.FieldType.IsPointer)
                {
                    continue;
                }

                object? value;
                try
                {
                    value = field.GetValue(node);
                }
                catch
                {
                    continue;
                }

                if (IsTraversable(value))
                {
                    yield return (value!, $"{type.Name}.{field.Name}");
                }
            }

            type = type.BaseType;
        }
    }

    private static bool IsTraversable(object? value)
    {
        if (value is null || value is string || value is Type)
        {
            return false;
        }

        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || type.IsPointer)
        {
            return false;
        }

        return true;
    }
}
