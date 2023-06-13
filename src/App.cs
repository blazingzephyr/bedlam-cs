using System;
using System.Collections;
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
        var env =
            Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process)
              as Hashtable ??
          throw new NullReferenceException(
              "This process does not have any environment variables.");

        void getVariable(string name, out string value)
        {
            if (env[name] is string variable)
            {
                value = variable;
                return;
            }

            throw new Exception(name);
        }

        getVariable("NOTION_INTEGRATION_TOKEN", out _integrationToken);
        getVariable("DISCORD_CLIENT_ID", out _client);
        getVariable("DISCORD_BOT_TOKEN", out _botToken);
        getVariable("DISCORD_BOT_OWNER_ID", out _botOwner);
        getVariable("DISCORD_LOG_CHANNEL_ID", out _log);

        string configBody = env["CONFIG"] is string config ? config : File.ReadAllText("config.json");
        _config = JsonSerializer.Deserialize<Config>(configBody);

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
