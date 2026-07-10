using ProjectHopeLynden.Application.Inventory;
using Xunit;

namespace ProjectHopeLynden.Application.Tests.Inventory;

public sealed class InventoryItemTotalViewTests
{
    [Fact]
    public void HasMixedCommodityStatus_ReturnsTrueWhenItemHasBothInventoryTypes()
    {
        var view = new InventoryItemTotalView(
            "Green Beans",
            OperationalTotalQuantity: 30,
            CommodityQuantity: 24,
            NonCommodityQuantity: 6,
            Entries: []);

        Assert.True(view.HasCommodityInventory);
        Assert.True(view.HasNonCommodityInventory);
        Assert.True(view.HasMixedCommodityStatus);
    }

    [Theory]
    [InlineData(24, 0)]
    [InlineData(0, 6)]
    [InlineData(0, 0)]
    public void HasMixedCommodityStatus_ReturnsFalseWhenItemDoesNotHaveBothInventoryTypes(
        int commodityQuantity,
        int nonCommodityQuantity)
    {
        var view = new InventoryItemTotalView(
            "Green Beans",
            commodityQuantity + nonCommodityQuantity,
            commodityQuantity,
            nonCommodityQuantity,
            Entries: []);

        Assert.Equal(commodityQuantity > 0, view.HasCommodityInventory);
        Assert.Equal(nonCommodityQuantity > 0, view.HasNonCommodityInventory);
        Assert.False(view.HasMixedCommodityStatus);
    }
}
