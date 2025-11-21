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
    public required string Lang { get; set; }

    [JsonPropertyName("traders")]
    public required Dictionary<string, JsonElement> Traders { get; set; }
}

[Injectable(TypePriority = OnLoadOrder.TraderRegistration + 99)]
public class Mod(
    ISptLogger<Mod> _logger,
    DatabaseService _db,
    LocaleService _locale,
    JsonUtil _json,
ImageRouter _imageRouter
) : IOnLoad
{
    protected Config? _config;
    protected string _modDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

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
        if (_config is null)
        {
            _logger.Error("Missing config.");
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
            if (trader.Base.Avatar is null)
            {
                continue;
            }

            string? name;
            if (!locale.TryGetValue($"{id} Nickname", out name))
            {
                continue;
            }

            var avatar = trader.Base.Avatar.Split('.')[0].Split('/').Last();
            traders[avatar] = traders[name] = traders[id] = trader.Base;
        }

        foreach (var file in Directory.EnumerateFiles(System.IO.Path.Join(_modDir, "avatars")))
        {
            var nameOrId = System.IO.Path.GetFileNameWithoutExtension(file);

            TraderBase? traderBase;
            if (!traders.TryGetValue(nameOrId, out traderBase))
            {
                _logger.Warning($"{nameOrId} not found, not changing avatar.");
                continue;
            }

            if (traderBase.Avatar is not null)
            {
                var extIndex = traderBase.Avatar.LastIndexOf('.');
                _imageRouter.AddRoute(traderBase.Avatar.Substring(0, extIndex), file);
            }
            else
            {
                _logger.Warning($"{nameOrId} avatar not set yet.");
            }

            // string name = GetTraderNick(locale, traderBase, nameOrId);
            // _logger.Info($"Changing {name}'s avatar.");
        }

        Dictionary<string, Dictionary<string, string>> allTraderDetails = new();
        foreach (var (nameOrId, configDetails) in _config.Traders)
        {
            TraderBase? traderBase;
            if (!traders.TryGetValue(nameOrId, out traderBase))
            {
                _logger.Warning($"{nameOrId} not found, not changing details.");
                continue;
            }

            var details = new Dictionary<string, string> { };
            var badDetails = false;
            if (configDetails.ValueKind == JsonValueKind.String)
            {
                details["Nickname"] = configDetails.GetString()!;
            }
            else if (configDetails.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in configDetails.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.String)
                    {
                        badDetails = true;
                        continue;
                    }
                    details[prop.Name] = prop.Value.GetString()!;
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

            var name = GetTraderNick(locale, traderBase, nameOrId);
            foreach (var (detail, value) in details)
            {
                // _logger.Info($"Changing {name}'s {detail} to {value}.");
            }
            allTraderDetails.Add(traderBase.Id, details);
        }
        UpdateLocales(allTraderDetails);

        return Task.CompletedTask;
    }

    protected void UpdateLocales(Dictionary<string, Dictionary<string, string>> traderDetails)
    {
        foreach (var lazyLoc in _db.GetLocales().Global.Values)
        {
            lazyLoc.AddTransformer(loc =>
            {
                foreach (var (traderId, details) in traderDetails)
                {
                    foreach (var (detail, value) in details)
                    {
                        loc![$"{traderId} {detail}"] = value;
                    }
                }
                return loc;
            });
        }
    }

    protected string GetTraderNick(Dictionary<string, string> loc, TraderBase trader, string fallback)
    {
        string? name;
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
    public override string ModGuid { get; init; } = "com.sgtlaggy.tradernamer";
    public override string Name { get; init; } = "Trader Namer";
    public override string Author { get; init; } = "sgtlaggy";
    public override SemanticVersioning.Version Version { get; init; } = new(Assembly.GetExecutingAssembly().GetName().Version!.ToString(3));
    public override string? Url { get; init; } = "https://github.com/sgtlaggy/spt-trader-namer";
    public override string License { get; init; } = "MIT";
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Contributors { get; init; }
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override bool? IsBundleMod { get; init; }
}
