using System.Collections.Generic;

public interface IItemTestable
{
    ServiceContainer GetContainer { get; }
}  
public static class ItemTestableExtensions
{
    public static void LoadItems(this IItemTestable target, List<TestItem> testItems)
    {
        var playerBag = target.GetContainer.Resolve<PlayerBagHandler>();
        playerBag.allItems.Clear();
        foreach (var itemData in testItems)
        {
            var newItem = InstanceFactory.CreateItem(itemData.itemAsset);
            newItem.quantity = itemData.quantity;
            playerBag.AddItem(newItem);
        }
    }
}