using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Cvars;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Builders;
using Newtonsoft.Json;
using WestReportSystemApiReborn;

namespace WestReportToDiscordReborn
{
    public class WestReportToDiscordReborn : BasePlugin
    {
        public override string ModuleName => "WestReportToDiscord";
        public override string ModuleVersion => "v1.1";
        public override string ModuleAuthor => "E!N";

        private IWestReportSystemApi? WRS_API;
        private IDiscordUtilitiesAPI? DiscordUtilities;
        private DiscordConfig? _config;

        private ulong channelId;
        private string[]? admins;
        private string? siteLink;

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            string configDirectory = GetConfigDirectory();
            EnsureConfigDirectory(configDirectory);
            string configPath = Path.Combine(configDirectory, "DiscordConfig.json");
            _config = DiscordConfig.Load(configPath);

            DiscordUtilities = new PluginCapability<IDiscordUtilitiesAPI>("discord_utilities").Get();
            WRS_API = IWestReportSystemApi.Capability.Get();

            if (DiscordUtilities == null || WRS_API == null)
            {
                Console.WriteLine($"{ModuleName} | Error: Essential services (WestReportSystem API and/or Discord Utilities) are not available.");
            }
            else
            {
                InitializeDiscord();
            }
        }

        private static string GetConfigDirectory()
        {
            return Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/configs/plugins/WestReportSystem/Modules");
        }

        private void EnsureConfigDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"{ModuleName} | Created configuration directory at: {directoryPath}");
            }
        }

        private void InitializeDiscord()
        {
            if (_config == null)
            {
                Console.WriteLine($"{ModuleName} | Error: Configuration is not loaded.");
                return;
            }
            channelId = _config.DiscordChannelId;
            admins = _config.DiscordAdmins;
            siteLink = WRS_API?.GetConfigValue<string>("SiteLink") ?? "";
            bool? timestamp = _config.DiscordFooterTimestamp;

            if (channelId == 0 || admins == null || siteLink == null || timestamp == null)
            {
                Console.WriteLine($"{ModuleName} | Error: Some configuration settings - ChannelID: {channelId}, Admins: {admins}, SiteLink: {siteLink}, FooterTimestamp: {timestamp} are missing.");
                return;
            }

            WRS_API?.RegisterReportingModule(WRS_SendReport_To_Discord);
            Console.WriteLine($"{ModuleName} | Initialized successfully.");
        }

        public void WRS_SendReport_To_Discord(CCSPlayerController sender, CCSPlayerController violator, string reason)
        {
            var serverName = ConVar.Find("hostname")?.StringValue ?? "Unknown Server";
            var mapName = NativeAPI.GetMapName();
            var serverIp = ConVar.Find("ip")?.StringValue ?? "Unknown IP";
            var serverPort = ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString() ?? "Unknown Port";
            int reportCount = WRS_API?.WRS_GetReportCounterPerRound(violator)?.GetValueOrDefault(violator, 1) ?? 1;

            if (siteLink != null && admins != null && WRS_API != null)
            {
                bool sender_prime = WRS_API.HasPrimeStatus(sender.SteamID);
                bool violator_prime = WRS_API.HasPrimeStatus(sender.SteamID);
                try
                {
                    var embedBuilder = new Embeds.Builder
                    {
                        Title = WRS_API?.GetTranslatedText("wrtd.EmbedAuthor") ?? "",
                        Description = WRS_API?.GetTranslatedText("wrtd.Title") ?? "",
                        ThumbnailUrl = WRS_API?.GetTranslatedText("wrtd.ThumbnailURL") ?? "",
                        Color = WRS_API?.GetTranslatedText("wrtd.EmbedColor") ?? "",
                        ImageUrl = WRS_API?.GetTranslatedText("wrtd.ImageURL") ?? "",
                        Footer = WRS_API?.GetTranslatedText("wrtd.FooterText") ?? "",
                        FooterTimestamp = WRS_API?.GetConfigValue<bool>("DiscordFooterTimestamp") ?? true,
                    };

                    embedBuilder.Fields.Add(new Embeds.FieldsData
                    {
                        Title = WRS_API?.GetTranslatedText("wrtd.Server") ?? "",
                        Description = serverName,
                        Inline = false
                    });
                    embedBuilder.Fields.Add(new Embeds.FieldsData
                    {
                        Title = WRS_API?.GetTranslatedText("wrtd.Administrators") ?? "",
                        Description = string.Join(", ", admins),
                        Inline = false
                    });
                    embedBuilder.Fields.Add(new Embeds.FieldsData
                    {
                        Title = WRS_API?.GetTranslatedText("wrtd.SenderTitle") ?? "",
                        Description = WRS_API?.GetTranslatedText("wrtd.SenderDescription", sender.PlayerName, sender.SteamID, sender_prime ? WRS_API.GetTranslatedText("wrs.PrimeTrue") : WRS_API.GetTranslatedText("wrs.PrimeFalse") ?? "Unknown") ?? "",
                        Inline = true
                    });
                    embedBuilder.Fields.Add(new Embeds.FieldsData
                    {
                        Title = WRS_API?.GetTranslatedText("wrtd.ViolatorTitle") ?? "",
                        Description = WRS_API?.GetTranslatedText("wrtd.ViolatorDescription", violator.PlayerName, violator.SteamID, violator_prime ? WRS_API.GetTranslatedText("wrs.PrimeTrue") : WRS_API.GetTranslatedText("wrs.PrimeFalse") ?? "Unknown") ?? "",
                        Inline = true
                    });
                    embedBuilder.Fields.Add(new Embeds.FieldsData
                    {
                        Title = WRS_API?.GetTranslatedText("wrtd.ComplaintReason") ?? "",
                        Description = $"`{reason}`",
                        Inline = false
                    });
                    embedBuilder.Fields.Add(new Embeds.FieldsData
                    {
                        Title = WRS_API?.GetTranslatedText("wrtd.Map") ?? "",
                        Description = WRS_API?.GetTranslatedText("wrtd.MapDescription", mapName, reportCount) ?? "",
                        Inline = false
                    });
                    embedBuilder.Fields.Add(new Embeds.FieldsData
                    {
                        Title = WRS_API?.GetTranslatedText("wrtd.ConnectToServerTitle") ?? "",
                        Description = WRS_API?.GetTranslatedText("wrtd.ConnectToServerDescription", serverIp, serverPort, siteLink) ?? "",
                        Inline = false
                    });

                    DiscordUtilities?.SendCustomMessageToChannel("report_system", channelId, null, embedBuilder, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ModuleName} | Error sending report to Discord: {ex.Message}");
                }
            }
        }

        public class DiscordConfig
        {
            public ulong DiscordChannelId { get; set; } = 0;
            public string[]? DiscordAdmins { get; set; } = ["<@719457093702123601>"];
            public bool DiscordFooterTimestamp { get; set; } = true;

            public static DiscordConfig Load(string configPath)
            {
                if (!File.Exists(configPath))
                {
                    DiscordConfig defaultConfig = new();
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(defaultConfig, Newtonsoft.Json.Formatting.Indented));
                    return defaultConfig;
                }

                string json = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<DiscordConfig>(json) ?? new DiscordConfig();
            }
        }
    }
}