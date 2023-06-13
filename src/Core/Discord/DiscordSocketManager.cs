
using Discord.WebSocket;
using Discord;

internal class DiscordSocketManager
{
    public record struct DiscordSocketManagerOptions
    (DiscordSocketConfig Config, TokenType TokenType, bool SendStartupMessage);

    public delegate void DiscordSocketEventHandler(DiscordSocketManager source);
    public event DiscordSocketEventHandler? OnKilled;

    public DiscordSocketClient Client { get; }
    readonly string _token;
    readonly TokenType _tokenType;

    public DiscordSocketManager(string token,
                                DiscordSocketManagerOptions options)
    {
        Client = new DiscordSocketClient(options.Config);
        _token = token;
        _tokenType = options.TokenType;

        Client.LoggedIn += async () =>
        {
            if (options.SendStartupMessage)
            {

            }
        };

        Client.MessageReceived += async message =>
        {
            if (message.Content == "Kill")
            {
                OnKilled?.Invoke(this);
                await Client.StopAsync();
            }
        };
    }

    public async Task Start()
    {
        await Client.LoginAsync(_tokenType, _token);
        await Client.StartAsync();
    }
}