# Project Configuration

To make this project functional in the editor and on a device, some initial setup is required.

## Application Configuration

To run the project and use platform services, create an application on the [Meta Quest Developer Center](https://developers.meta.com/horizon/).

For device operation, a Quest application is needed, and for editor operation, a PCVR application is required. The following sections describe the necessary configuration.

### Data Use Checkup

To use platform features, request the necessary data in the _Data Use Checkup_ section of the application.

![data use checkup](./Media/dashboard/datausecheckup.png "Data use Checkup")

Configure the required Data Usage:
* **User Id**: Avatars, Destinations, Multiplayer, Oculus Username, Friends Invites, User Invite Others
* **In-App Purchases**: IAP
* **User Profile**: Avatars
* **Avatars**: Avatars
* **Deep Linking**: Destinations
* **Friends**: Multiplayer
* **Blocked Users**: Other - we use the Blocking API
* **Invites**: Multiplayer, Friends Invite, User Invite Others

### Add-ons

This application integrates in-app purchases (IAP) to demonstrate durable and consumable purchases. Here are the expected configurations.

First, open the Add-ons from the Platform Services:

![Platform Services Add-ons](./Media/dashboard/dashboard_addons_platformservices.png "Platform Services Add-ons")

Then, set up the different Add-ons.

![Add-ons](./Media/dashboard/dashboard_addons.png "Add-ons")

We created 3 durable SKUs, which can be purchased once, for the icons. We also created 1 consumable SKU, which can be purchased multiple times, for the pet cat.

### Destinations

This application uses Destination configuration to enable users to invite friends to the same arenas and launch the application together.

First, open the Destinations from the Platform Services:

![Platform Services Destinations](./Media/dashboard/dashboard_destinations_platformservices.png "Platform Services Destinations")

Then, set up the different destinations.

![Destinations](./Media/dashboard/dashboard_destinations.png "Destinations")

#### Main Menu

The MainMenu destination is unique to the user, where no other player can join. It is set up as follows:

![Destination Main Menu](./Media/dashboard/dashboard_destination_mainmenu.png "Destination Main Menu")

The deeplink type is set to DISABLED, preventing users from joining this destination together.

#### Arenas

Different Arenas exist for each region:

![Destination NA](./Media/dashboard/dashboard_destination_na.png "Destination NA")

This example is for the North American region. The deeplink type is ENABLE, and the deeplink data specifies the Photon region. The deeplink message format is specific to our project. The Group Launch capacity is set to a maximum of 6 players per Arena.

Here is a table for destination settings:

<div style="margin: auto; padding: 10pt;">
<table>
<tr>
    <th>Destination</th>
    <th>API Name</th>
    <th>Deeplink Message</th>
</tr>
<tr>
    <td><b>Main Menu</b></td>
	<td>MainMenu</td>
	<td><i>N/A</i></td>
</tr>
<tr>
	<td><b>North America</b></td>
	<td>Arena</td>
	<td>{"Region":"usw"}</td>
</tr>
<tr>
    <td><b>South America</b></td>
	<td>ArenaSA</td>
	<td>{"Region":"sa"}</td>
</tr>
<tr>
	<td><b>Japan</b></td>
	<td>ArenaJP</td>
	<td>{"Region":"jp"}</td>
</tr>
<tr>
    <td><b>Europe</b></td>
	<td>ArenaEU</td>
	<td>{"Region":"eu"}</td>
</tr>
<tr>
	<td><b>Australia</b></td>
	<td>ArenaAU</td>
	<td>{"Region":"au"}</td>
</tr>
<tr>
    <td><b>Asia</b></td>
	<td>ArenaAsia</td>
	<td>{"Region":"asia"}</td>
</tr>
</table>
</div>

### Set the Application ID

Set the application ID in Unity.

The identifier (__App ID__) is found in the _API_ section.

![Application API](./Media/dashboard/dashboard_api.png "Application API")

Place it in [Assets/Resources/OculusPlatformSettings.asset](../Assets/Resources/OculusPlatformSettings.asset).

![Oculus Platform Settings Menu](./Media/editor/oculusplatformsettings_menu.png "Oculus Platform Settings Menu")

![Oculus Platform Settings](./Media/editor/oculusplatformsettings.png "Oculus Platform Settings")

## Photon Configuration

To get the sample working, configure Photon with your account and applications. The Photon base plan is free.
- Visit [photonengine.com](https://www.photonengine.com) and [create an account](https://doc.photonengine.com/realtime/current/getting-started/obtain-your-app-id).
- From your Photon dashboard, click "Create A New App."
  - Create 2 apps: "Realtime" and "Voice."
- Fill out the form, setting the type to "Photon Realtime," then click Create.
- Fill out the form, setting the type to "Photon Voice," then click Create.

Your new app will now show on your Photon dashboard. Click the App ID to reveal the full string and copy the value for each app.

Open your Unity project and paste your Realtime App ID in [Assets/Photon/Resources/PhotonAppSettings.asset](../Assets/Photon/Resources/PhotonAppSettings.asset).

![Photon App Settings Location](./Media/editor/photonappsettings_location.png "Photon App Settings Location")

![Photon App Settings](./Media/editor/photonappsettings.png "Photon App Settings")

Set the Voice App Id on the VoiceRecorder prefab:

![Photon Voice Recorder Location](./Media/editor/photonvoicerecorder_location.png "Photon Voice Recorder Location")

![Photon Voice Settings](./Media/editor/photonvoicesetting.png "Photon Voice Settings")

### Additional Setup for Realtime

To allow players to play in different regions with reduced latency, we used the different regions Photon provides. To simplify the project, we limited the number of regions used. Whitelist the regions used by the application.

![Photon Whitelist](./Media/photon_whitelist.png "Photon Whitelist")

The Photon Realtime transport should now work. Verify network traffic on your Photon account dashboard.

## Upload to Release Channel

To use platform features, upload an initial build to a release channel. For instructions, visit the [developer center](https://developers.meta.com/horizon/resources/publish-release-channels-upload/). To test with other users, add them to the channel; more information is in the [Add Users to Release Channel](https://developers.meta.com/horizon/resources/publish-release-channels-add-users/) topic.

Once the initial build is uploaded, you can use any development build with the same application ID; there is no need to upload every build to test local changes.
