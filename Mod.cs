using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services;


namespace TraderNamer;

public record Config
{
    [JsonPropertyName("lang")]
    public string Lang { get; set; }

    [JsonPropertyName("traders")]
    public Dictionary<string, JsonElement> Traders { get; set; }
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class TraderNamer(
    ISptLogger<TraderNamer> _logger,
    DatabaseService _db,
    LocaleService _locale,
    JsonUtil _json,
    ImageRouter _imageRouter
) : IOnLoad
{
    protected Config _config;
    protected string _modDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    public Task OnLoad()
    {
        try
        {
            _config = _json.DeserializeFromFile<Config>(System.IO.Path.Join(_modDir, "config.json"));
        }
        catch (JsonException)
        {
            _logger.Error("Invalid config.");
            return Task.CompletedTask;
        }

        Dictionary<string, string> locale;
        try
        {
            locale = _locale.GetLocaleDb(_config.Lang);
        }
        catch
        {
            _logger.Warning($"Selected locale {_config.Lang} is invalid, using default.");
            locale = _locale.GetLocaleDb();
        }

        var traders = new Dictionary<string, TraderBase>();
        foreach (var (id, trader) in _db.GetTraders())
        {
            var name = locale[$"{id} Nickname"];
            var avatar = trader.Base.Avatar.Split('.')[0].Split('/').Last();
            traders[avatar] = traders[name] = traders[id] = trader.Base;
        }

        foreach (var file in Directory.EnumerateFiles(System.IO.Path.Join(_modDir, "avatars")))
        {
            var nameOrId = System.IO.Path.GetFileNameWithoutExtension(file);

            TraderBase trader;
            if (!traders.TryGetValue(nameOrId, out trader))
            {
                _logger.Warning($"{nameOrId} does not exist, not changing avatar.");
                continue;
            }

            // TODO: may need to remove extension from avatar path
            _imageRouter.AddRoute(trader.Avatar, file);

            string name = GetTraderNick(locale, trader, nameOrId);
            _logger.Info($"Changing {name}'s avatar.");
        }

        foreach (var (nameOrId, configDetails) in _config.Traders)
        {
            TraderBase trader;
            if (!traders.TryGetValue(nameOrId, out trader))
            {
                _logger.Warning($"{nameOrId} does not exist, not changing details.");
                continue;
            }

            var details = new Dictionary<string, string> { };
            var badDetails = false;
            if (configDetails.ValueKind == JsonValueKind.String)
            {
                details["Nickname"] = configDetails.GetString();
            }
            else if (configDetails.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in configDetails.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.String)
                    {
                        badDetails = true;
                    }
                    details[prop.Name] = prop.Value.GetString();
                }
            }
            else
            {
                badDetails = true;
            }
            if (badDetails)
            {
                _logger.Warning($"Invalid details provided for {nameOrId}.");
                continue;
            }

            var name = GetTraderNick(locale, trader, nameOrId);
            foreach (var (detail, value) in details)
            {
                _logger.Info($"Changing {name}'s {detail} to {value}.");
            }
            foreach (var loc in _db.GetLocales().Global.Values)
            {
                foreach (var (detail, value) in details)
                {
                    loc.Value[$"{trader.Id} {detail}"] = value;
                }
            }
        }

        return Task.CompletedTask;
    }

    protected string GetTraderNick(Dictionary<string, string> loc, TraderBase trader, string fallback)
    {
        string name;
        if (loc.TryGetValue(trader.Id, out name))
        {
            return name;
        }

        if (!String.IsNullOrWhiteSpace(trader.Nickname))
        {
            return trader.Nickname;
        }

        return fallback;
    }
}

public record ModMetadata : AbstractModMetadata
{
    public override string Name { get; set; } = "Trader Namer";
    public override string Author { get; set; } = "sgtlaggy";
    public override string Version { get; set; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    public override string Url { get; set; } = "https://github.com/sgtlaggy/spt-trader-namer";
    public override string Licence { get; set; } = "MIT";
    public override string SptVersion { get; set; } = "~4.0.0";
    public override List<string> Contributors { get; set; }
    public override List<string> LoadBefore { get; set; }
    public override List<string> LoadAfter { get; set; }
    public override List<string> Incompatibilities { get; set; }
    public override Dictionary<string, string> ModDependencies { get; set; }
    public override bool? IsBundleMod { get; set; }
}
