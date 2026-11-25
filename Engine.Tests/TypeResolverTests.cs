using Engine.Core.ECS.Components;
using Engine.Core.Runtime;
using Engine.Core.Utilities;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Engine.Tests;

public class TypeResolverTests
{
    [Fact]
    public void FindTypeResolvesAssemblyQualifiedFullAndShortNames()
    {
        Assert.Equal(
            typeof(TileDataComponent),
            TypeResolver.FindType(typeof(TileDataComponent).AssemblyQualifiedName!));
        Assert.Equal(
            typeof(TileDataComponent),
            TypeResolver.FindType(typeof(TileDataComponent).FullName!));
        Assert.Equal(
            typeof(TileDataComponent),
            TypeResolver.FindType("tiledatacomponent", typeof(DataComponent)));
        Assert.Null(TypeResolver.FindType(nameof(ResolverProbe), typeof(DataComponent)));
        Assert.Null(TypeResolver.FindType("Type.That.Does.Not.Exist"));
    }

    [Fact]
    public void FindTypeRejectsAmbiguousShortNames()
    {
        Assert.Null(TypeResolver.FindType(nameof(CollisionA.DuplicateResolverProbe), typeof(DataComponent)));
    }

    [Fact]
    public void ClearCacheDropsStaleTypeResolutions()
    {
        var cacheField = typeof(TypeResolver).GetField(
            "_typeCache",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(cacheField);

        var cache = (Dictionary<string, Type>)cacheField!.GetValue(null)!;
        cache[nameof(ResolverProbe)] = typeof(string);

        Assert.Equal(typeof(string), TypeResolver.FindType(nameof(ResolverProbe)));

        TypeResolver.ClearCache();

        Assert.Equal(typeof(ResolverProbe), TypeResolver.FindType(nameof(ResolverProbe)));
    }

    private static class CollisionA
    {
        public sealed class DuplicateResolverProbe : DataComponent
        {
            public override string DisplayName
            {
                get;
                set;
            } = "A";
        }
    }

    private static class CollisionB
    {
        public sealed class DuplicateResolverProbe : DataComponent
        {
            public override string DisplayName
            {
                get;
                set;
            } = "B";
        }
    }
}
