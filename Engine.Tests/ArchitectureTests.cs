using System.Reflection;
using Engine.Core.ECS;
using Xunit;

namespace Engine.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void EngineCoreDoesNotContainEditorContextManager()
    {
        var engineTypes = typeof(GameScene).Assembly.GetTypes();

        Assert.DoesNotContain(engineTypes, type => type.Name == "EditorContextManager");
    }

    [Fact]
    public void EngineCoreDoesNotReferenceEditorAssemblies()
    {
        var references = typeof(GameScene).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, reference =>
            reference.Name?.StartsWith("Editor", StringComparison.OrdinalIgnoreCase) == true);
    }
}
