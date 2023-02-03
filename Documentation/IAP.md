# In-app Purchases (IAP)
To demonstrate how to integrate IAP in a project we implemented a simple store in Ultimate Gloveball.

In the store you can purchase an icon permanently (durable) or some pet cats that would need to be repurchased once used (consumable).
![Store](./Media/mainmenu_store.png)
![Store Purchase Flow](./Media/mainmenu_store_flow.png)

## Implementation
We based our implementation on the [IAP developer resource](https://developer.oculus.com/documentation/unity/ps-iap/).

First we have a game agnostic [IAPManager](../Assets/UltimateGloveBall/Scripts/App/IAPManager.cs) that wraps around the platform IAP logic to handle fetching the product and purchases information as well as consuming purchases for consumables. This is meant to supply reusable logic for any type of project that wants to implement IAP. We added support to categorize products so that we can easily get all product for a given category.

Then we have the [StoreMenuController](../Assets/UltimateGloveBall/Scripts/MainMenu/StoreMenuController.cs) which implements the logic on how to buy products and consume consumable purchases. The icons are loaded using the product data fetch from the IAPManager.

The fetch logic to get the products and purchases information can be found in the [UBGApplication](../Assets/UltimateGloveBall/Scripts/App/UGBApplication.cs) initialization code. Where we fetch consumables and durable product with different categories associated to them.

## Configuration
See the [Add-ons section of the Configuration page](Configuration.md#add-ons) for details.