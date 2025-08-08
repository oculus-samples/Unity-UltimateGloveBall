# In-App Purchases (IAP)

To show how to integrate IAP into a project, we created a simple store in Ultimate Gloveball.

In the store, you can buy a permanent icon (durable) or pet cats that need repurchasing after use (consumable).

![Store](./Media/mainmenu_store.png)
![Store Purchase Flow](./Media/mainmenu_store_flow.png)

## Implementation

Our implementation is based on the [IAP developer resource](https://developers.meta.com/horizon/documentation/unity/ps-iap/).

We use a game-agnostic [IAPManager](../Assets/UltimateGloveBall/Scripts/App/IAPManager.cs) to handle the platform's IAP logic. It manages fetching product and purchase information and consuming purchases for consumables. This provides reusable logic for any project implementing IAP. We added product categorization to easily access all products in a category.

The [StoreMenuController](../Assets/UltimateGloveBall/Scripts/MainMenu/StoreMenuController.cs) manages buying products and consuming consumable purchases. Icons are loaded using product data from the IAPManager.

The logic to fetch product and purchase information is in the [UBGApplication](../Assets/UltimateGloveBall/Scripts/App/UGBApplication.cs) initialization code. Here, we fetch consumables and durable products with their associated categories.

## Configuration

Refer to the [Add-ons section of the Configuration page](Configuration.md#add-ons) for details.
