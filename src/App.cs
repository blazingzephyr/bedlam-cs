using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Bedlam.App;
using static DiscordSocketManager;

namespace Bedlam;

internal class App
{
    public record struct Config
    (DiscordSocketManagerOptions Discord);

    readonly CancellationTokenSource _source;
    readonly Config _config;
    readonly string _integrationToken;
    readonly string _client;
    readonly string _botToken;
    readonly string _botOwner;
    readonly string _log;

    public App()
    {
        var env = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process)
              as Dictionary<string, string> ??
          throw new NullReferenceException(
              "This process does not have any environment variables.");

        var getVariable = (string name, out string variable) => variable =
            env.ContainsKey(name) ? env[name] : throw new Exception(name);

        getVariable("NOTION_INTEGRATION_TOKEN", out _integrationToken);
        getVariable("DISCORD_CLIENT_ID", out _client);
        getVariable("DISCORD_BOT_TOKEN", out _botToken);
        getVariable("DISCORD_BOT_OWNER_ID", out _botOwner);
        getVariable("DISCORD_LOG_CHANNEL_ID", out _log);

        _config = JsonSerializer.Deserialize<Config>(
            env.ContainsKey("CONFIG") ? env["CONFIG"]
                                      : File.ReadAllText("config.json"));

        _source = new CancellationTokenSource();

        StartDiscord();

        _source.Token.WaitHandle.WaitOne();
    }

    public async Task StartDiscord()
    {
        DiscordSocketManager discord = new (_botToken, _config.Discord);
        discord.OnKilled += manager => _source.Cancel();

        await discord.Start();
    }
}
