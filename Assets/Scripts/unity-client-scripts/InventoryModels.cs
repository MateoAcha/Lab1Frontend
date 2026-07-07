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
    public string materialKey;
    public string material_key;
    public string weaponType;
    public string weaponColor;
    public string weapon_type;
    public string weapon_color;
    public string weaponSubtype;
    public string weapon_subtype;
    public string weaponClass;
    public string weapon_class;
    public bool equipped;
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
    public string weaponType;
    public string weaponColor;
    public string weapon_type;
    public string weapon_color;
    public string weaponSubtype;
    public string weapon_subtype;
    public string weaponClass;
    public string weapon_class;
    public int goldPrice;
    public int purchaseQuantity;
    public int skinId;
}

[Serializable]
public class ShopCatalogData
{
    public ShopItemData[] items;
}

[Serializable]
public class SkinData
{
    public int skinId;
    public string skinName;
    public string rarity;
    public bool equipped;
}

[Serializable]
public class UserSkinsData
{
    public int userId;
    public SkinData[] skins;
}

[Serializable]
public class PaymentPreferenceData
{
    public long paymentRecordId;
    public string checkoutUrl;
}

[Serializable]
public class PaymentStatusData
{
    public string status;
}
