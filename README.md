# WestReportToDiscord
Modular report system for your CS:2 server - WestReportSystem

**Sending reports to Discord**

![image](https://github.com/Stimayk/WestReportToDiscord/assets/51941742/8adada92-48db-447a-b079-ffe9718be3a9)


Uses [CS2-Discord-Utilities](https://github.com/NockyCZ/CS2-Discord-Utilities) Accordingly requires [customizations](https://github.com/NockyCZ/CS2-Discord-Utilities?tab=readme-ov-file#installation)

The WRTD module itself is also customized
Configuration file:
```
{
  "DiscordChannelId": 1068112921336483932, // Channel ID of the channel to which the report will be sent
  "DiscordAdmins": [
    "<@719457093702123601>"
  ], // Role <@&id> User <@id>
  "DiscordFooterTimestamp": true // true or false
}
```

Installing the module:
+ Download the archive from the [releases](https://github.com/Stimayk/WestReportToDiscord/releases)
+ Unzip to plugins
+ Customize CS2-Discord-Utilities and the module itself in /configs/plugins
+ Customize translations if necessary
