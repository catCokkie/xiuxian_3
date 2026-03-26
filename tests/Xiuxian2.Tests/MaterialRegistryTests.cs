using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class MaterialRegistryTests
{
    [Fact]
    public void Count_Returns16Materials()
    {
        Assert.Equal(16, MaterialRegistry.Count);
    }

    [Fact]
    public void TryGet_ReturnsKnownMaterial()
    {
        Assert.True(MaterialRegistry.TryGet("spirit_herb", out MaterialRegistry.MaterialDef def));
        Assert.Equal("灵草", def.DisplayName);
        Assert.Contains("dungeon", def.SourceSystems);
        Assert.Contains("garden", def.SourceSystems);
        Assert.Contains("alchemy", def.ConsumerSystems);
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForUnknown()
    {
        Assert.False(MaterialRegistry.TryGet("nonexistent_item", out _));
    }

    [Fact]
    public void GetAll_ContainsAllCombatDropMaterials()
    {
        Assert.True(MaterialRegistry.TryGet("lingqi_shard", out _));
        Assert.True(MaterialRegistry.TryGet("broken_talisman", out _));
        Assert.True(MaterialRegistry.TryGet("spirit_ink", out _));
        Assert.True(MaterialRegistry.TryGet("beast_bone", out _));
        Assert.True(MaterialRegistry.TryGet("toxic_gland", out _));
    }

    [Fact]
    public void GetAll_ContainsAllGatheringMaterials()
    {
        // Garden
        Assert.True(MaterialRegistry.TryGet("spirit_flower", out _));
        Assert.True(MaterialRegistry.TryGet("spirit_fruit", out _));
        Assert.True(MaterialRegistry.TryGet("seed", out _));
        // Mining
        Assert.True(MaterialRegistry.TryGet("cold_iron_ore", out _));
        Assert.True(MaterialRegistry.TryGet("spirit_jade", out _));
        Assert.True(MaterialRegistry.TryGet("mithril", out _));
        Assert.True(MaterialRegistry.TryGet("fossil", out _));
        // Fishing
        Assert.True(MaterialRegistry.TryGet("spirit_fish", out _));
        Assert.True(MaterialRegistry.TryGet("spirit_pearl", out _));
        Assert.True(MaterialRegistry.TryGet("dragon_saliva", out _));
    }

    [Fact]
    public void Materials_HaveValidSourceAndConsumer()
    {
        foreach (MaterialRegistry.MaterialDef mat in MaterialRegistry.GetAll())
        {
            Assert.False(string.IsNullOrEmpty(mat.ItemId), $"Material has empty ItemId");
            Assert.False(string.IsNullOrEmpty(mat.DisplayName), $"Material {mat.ItemId} has empty DisplayName");
            Assert.NotEmpty(mat.SourceSystems);
            Assert.NotEmpty(mat.ConsumerSystems);
        }
    }
}
