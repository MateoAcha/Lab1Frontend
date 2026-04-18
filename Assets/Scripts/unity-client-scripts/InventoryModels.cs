using System;

[Serializable]
public class InventoryItemData
{
    public int userInventoryId;
    public int itemId;
    public string itemName;
    public string itemType;
    public string rarity;
    public string description;
    public int quantity;
    public string acquiredAt;
    public string detailSummary;
}

[Serializable]
public class UserInventoryData
{
    public int userId;
    public InventoryItemData[] items;
}

[Serializable]
public class ShopItemData
{
    public int shopItemId;
    public int itemId;
    public string itemName;
    public string itemType;
    public string rarity;
    public string description;
    public string detailSummary;
    public int goldPrice;
    public int purchaseQuantity;
}

[Serializable]
public class ShopCatalogData
{
    public ShopItemData[] items;
}
