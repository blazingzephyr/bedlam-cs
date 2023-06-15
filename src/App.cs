
using System.Collections;
using System.Text.Json;
using static Bedlam.NotionManager;
using static DiscordSocketManager;

namespace Bedlam;

internal class App
{
    public record struct Config
    (DiscordSocketManagerOptions Discord, NotionManagerOptions Notion);

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

        getVariable("NOTION_INTEGRATION_TOKEN", out string integrationToken);
        getVariable("DISCORD_CLIENT_ID", out string client);
        getVariable("DISCORD_BOT_TOKEN", out string botToken);
        getVariable("DISCORD_BOT_OWNER_ID", out string botOwner);
        getVariable("DISCORD_LOG_CHANNEL_ID", out string log);

        string configBody = env["CONFIG"] is string configEnv ? configEnv : File.ReadAllText("config.json");
        Config config = JsonSerializer.Deserialize<Config>(configBody);
        CancellationTokenSource source = new CancellationTokenSource();

        DiscordSocketManager discord = new(botToken, UInt64.Parse(botOwner), config.Discord);
        discord.OnKilled += manager => source.Cancel();

        NotionManager notion = new(integrationToken, config.Notion);
        notion.OnReady += manager => discord.Client.Guilds.First().TextChannels.First().SendMessageAsync("I'M READY SORRY");
        notion.OnUpdated += (manager, a, b) => discord.Client.Guilds.First().TextChannels.First().SendMessageAsync(a.Count.ToString());
        notion.OnApiError += async (manager, error) =>
        {
            var channel = discord.Client.Guilds.First().TextChannels.First();
            await channel.SendMessageAsync("An exception has occured or some bs. Have a message lmao bye.");
            await channel.SendMessageAsync(error.Message);
        };

        discord.Start();
        notion.Watch(source.Token);

        Console.WriteLine("Waiting to be canceled...");
        source.Token.WaitHandle.WaitOne();
    }
}