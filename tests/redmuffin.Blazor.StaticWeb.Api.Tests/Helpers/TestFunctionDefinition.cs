using System.Collections.Immutable;
using JetBrains.Annotations;
using Microsoft.Azure.Functions.Worker;
using redmuffin.Blazor.StaticWeb.Api.Functions;
using Assembly = System.Reflection.Assembly;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

public class TestFunctionDefinition(string functionId) : FunctionDefinition
{
    public override IImmutableDictionary<string, BindingMetadata> InputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
    public override IImmutableDictionary<string, BindingMetadata> OutputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
    [UsedImplicitly] public override ImmutableArray<FunctionParameter> Parameters { get; }
    public override string PathToAssembly => Assembly.GetExecutingAssembly().Location;
    public override string EntryPoint => typeof(RaindropListVideos).FullName!;
    public override string Id { get; } = functionId;
    public override string Name { get; } = functionId;
}