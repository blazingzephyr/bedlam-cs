
using Discord;

namespace Bedlam;

public record MessageCreationOptions
(string? Text = null, bool IsTTS = false, Embed? Embed = null, RequestOptions? Options = null,
AllowedMentions? AllowedMentions = null, MessageReference? MessageReference = null,
MessageComponent? Components = null, ISticker[]? Stickers = null, Embed[]? Embeds = null,
MessageFlags Flags = MessageFlags.None);

internal static class MessageExtension
{
    public async static Task<IUserMessage> SendMessageAsync(this IMessageChannel channel, MessageCreationOptions options)
    {
        return await channel.SendMessageAsync(
            options.Text, options.IsTTS, options.Embed, options.Options,
            options.AllowedMentions, options.MessageReference, options.Components,
            options.Stickers, options.Embeds, options.Flags);
    }

    public async static Task<IUserMessage?> TrySendAsync(this IMessageChannel? channel, MessageCreationOptions? options)
    {
        if (channel != null && options != null)
        {
            return await channel.SendMessageAsync(options);
        }
        else
        {
            return null;
        }
    }
}