
using Newtonsoft.Json;
using System.Collections;
using static Bedlam.NotionManager;
using static DiscordSocketManager;

namespace Bedlam;

internal class App
{
    public record struct Config
    (DiscordSocketManagerOptions Discord, NotionManagerOptions Notion);

    string? _integrationToken;
    string? _client;
    string? _botToken;
    string? _botOwner;
    string? _log;
    string? _configBody;

    public App()
    {
        LoadEnvironmentVariables();

        Config config = JsonConvert.DeserializeObject<Config>(_configBody!);
        CancellationTokenSource source = new ();

        DiscordSocketManager discord = new(_botToken!, UInt64.Parse(_botOwner!), UInt64.Parse(_log!), config.Discord);
        discord.OnKilled += manager => source.Cancel();

        NotionManager notion = new(_integrationToken!, config.Notion);
        notion.OnReady += manager =>
        {
            discord.Client.Guilds.First().TextChannels.First().SendMessageAsync("I'M READY SORRY");
        };

        notion.OnUpdated += (manager, a, b) =>
        {
            discord.Client.Guilds.First().TextChannels.First().SendMessageAsync(a.Count.ToString());
        };

        notion.OnApiError += async (manager, error) =>
        {
            var channel = discord.Client.Guilds.First().TextChannels.First();
            await channel.SendMessageAsync("An exception has occured or some bs. Have a message lmao bye.");
            await channel.SendMessageAsync(error.Message);
        };

        discord.Start(source.Token);
        notion.Watch(source.Token);

        Console.WriteLine("Waiting to be canceled...");
        source.Token.WaitHandle.WaitOne();
    }

    private void LoadEnvironmentVariables()
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

        _configBody = env["CONFIG"] is string configEnv ? configEnv : File.ReadAllText("config.json");
    }
}