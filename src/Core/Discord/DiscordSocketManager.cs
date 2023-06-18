
using Discord.WebSocket;
using Discord;
using System.Threading;

namespace Bedlam;

internal class DiscordSocketManager
{
    public record struct DiscordSocketManagerOptions
    (DiscordSocketConfig Config, TokenType TokenType, bool SendStartupMessage,
    ulong? StartupChannel, MessageCreationOptions? StartupMessage, MessageCreationOptions? KillMessage,
    MessageCreationOptions? KillFail);

    public delegate void DiscordSocketEventHandler(DiscordSocketManager source);
    public event DiscordSocketEventHandler? OnKilled;

    public DiscordSocketClient Client { get; }
    public ITextChannel? Log { get; private set; }
    public ITextChannel? StartupChannel { get; private set; }

    readonly string _token;
    readonly TokenType _tokenType;
    readonly ulong _botOwner;

    CancellationToken _cancellationToken;

    public DiscordSocketManager(string token, ulong botOwner, ulong log,
                                DiscordSocketManagerOptions options)
    {
        Client = new DiscordSocketClient(options.Config);
        _token = token;
        _tokenType = options.TokenType;
        _botOwner = botOwner;

        Client.LoggedIn += async () =>
        {
            var requestOptions = new RequestOptions { CancelToken = _cancellationToken };
            Log = await Client.GetChannelAsync(log, requestOptions) as ITextChannel;

            if (options.SendStartupMessage)
            {
                StartupChannel = options.StartupChannel == null ? Log :
                await Client.GetChannelAsync(options.StartupChannel.Value, requestOptions) as ITextChannel;

                MessageCreationOptions? startup = options.StartupMessage;
                await StartupChannel.TrySendAsync(startup);
            }
        };

        Client.InteractionCreated += async interaction =>
        {
            if (interaction is IApplicationCommand command && command.Name == "kill")
            {
                if (interaction.User.Id == _botOwner)
                {
                    await interaction.Channel.TrySendAsync(options.KillMessage);
                    await Client.StopAsync();
                    await Client.LogoutAsync();
                    OnKilled?.Invoke(this);
                }
                else
                {
                    await interaction.Channel.TrySendAsync(options.KillFail);
                }
            }
        };
    }

    public async Task Start(CancellationToken token)
    {
        _cancellationToken = token;
        await Client.LoginAsync(_tokenType, _token);
        await Client.StartAsync();
    }
}