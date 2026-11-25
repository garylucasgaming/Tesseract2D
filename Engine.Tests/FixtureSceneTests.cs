using Engine.Core.ECS;
using Engine.Core.ECS.Components.UI;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Xunit;

namespace Engine.Tests;

public class FixtureSceneTests
{
    [Fact]
    public void FontsSceneResolvesRealComponentAndShortNameScriptReferences()
    {
        var dto = FixtureYaml.Load<SceneDataDto>("Content/Assets/Scenes/Main.scene");

        Assert.Equal("Main", dto.SceneName);
        Assert.Equal(Guid.Parse("42a453b3-0f0d-49c4-95f7-03d2b43aab6e"), Guid.Parse(dto.Id));
        Assert.Equal(new[] { "Main_TestMap.yaml" }, dto.TileMapFiles);
        Assert.Equal(7, dto.Entities.Count);

        var entities = FixtureScene.ImportAll(dto, liveEntities =>
        {
            // Lifecycle hooks populate these before reference resolution; clear them so
            // the assertions below prove the serialized $ref payloads were applied.
            Find(liveEntities, "canvas").GetComponent<UICanvasComponent>()!.Hovered = null;
            Find(liveEntities, "Listbox").GetComponent<ListBoxComponent>()!.ItemsPanel = null;
        });
        var canvas = Find(entities, "canvas").GetComponent<UICanvasComponent>();
        var listboxEntity = Find(entities, "Listbox");
        var listBox = listboxEntity.GetComponent<ListBoxComponent>();
        var layout = listboxEntity.GetComponent<LayoutComponent>();
        var populator = Find(entities, "listboxPopulator").GetComponent<ListboxPopulater>();

        Assert.NotNull(canvas);
        Assert.NotNull(listBox);
        Assert.NotNull(layout);
        Assert.NotNull(populator);

        Assert.Equal("Health", Find(entities, "label").GetComponent<LabelComponent>()!.Text);
        Assert.Equal(4, canvas.ChildElements.Count);
        Assert.Contains(canvas.ChildElements, c => c is LabelComponent && c.gameObject?.Name == "label");
        Assert.Contains(canvas.ChildElements, c => c is LayoutComponent && c.gameObject?.Name == "Listbox");
        Assert.Contains(canvas.ChildElements, c => c is PanelComponent && c.gameObject?.Name == "Panel");
        Assert.Contains(canvas.ChildElements, c => c is UIImageComponent && c.gameObject?.Name == "Guy");

        Assert.Same(layout, listBox.ItemsPanel);
        Assert.Same(listBox, populator.listbox);

        Assert.IsType<GameEvent>(canvas.Hovered);
        Assert.Equal(string.Empty, canvas.Hovered!.MethodName);
        Assert.Null(canvas.Hovered.TargetGameObject);
    }

    private static GameObject Find(List<GameObject> entities, string name) =>
        entities.Single(entity => entity.Name == name);
}
