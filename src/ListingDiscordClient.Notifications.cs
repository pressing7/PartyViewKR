using Discord;
using Discord.WebSocket;

namespace PartyViewKR;

internal sealed partial class ListingDiscordClient
{
    private ListingDiscordNotifications? notifications;
    internal string NotificationStatus => notifications?.Status ?? "디스코드 알림 연결 준비 중입니다.";
    internal void ObserveCompletion(bool enabled, ulong self, long revision, bool complete, bool excludesSingleJob = false) =>
        notifications?.Observe(enabled && Connected, self, revision, complete, excludesSingleJob);

    internal static AllowedMentions CompletionMentions() => new(AllowedMentionTypes.Everyone);
    private sealed class NotificationTransport(ListingDiscordClient client) : IDiscordNotificationTransport
    {
        public bool Connected => client.Connected;
        private SocketTextChannel? Channel(DiscordNotificationTarget target) =>
            client.socket.GetGuild(target.GuildId)?.GetTextChannel(target.ChannelId);

        public Task<string?> CheckChannel(DiscordNotificationTarget target, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if (!Connected) return Task.FromResult<string?>("봇 연결을 기다려 주세요.");
            var channel = Channel(target);
            if (channel == null) return Task.FromResult<string?>("서버의 일반 텍스트 채널에서 설정하고 봇이 채널을 볼 수 있는지 확인해 주세요.");
            var permissions = channel.Guild.CurrentUser.GetPermissions(channel);
            return Task.FromResult<string?>(!permissions.ViewChannel || !permissions.SendMessages
                ? "봇에 이 채널의 ‘채널 보기’와 ‘메시지 보내기’ 권한이 필요합니다."
                : !permissions.MentionEveryone ? "봇에 이 채널의 ‘@everyone, @here 및 모든 역할 멘션’ 권한이 필요합니다." : null);
        }

        public async Task SendCompletion(DiscordNotificationTarget target, CancellationToken cancellation, bool excludesSingleJob = false)
        {
            var error = await CheckChannel(target, cancellation);
            if (error != null) throw new InvalidOperationException("Notification channel unavailable");
            cancellation.ThrowIfCancellationRequested();
            var sent = await Channel(target)!.SendMessageAsync(excludesSingleJob ? ListingDiscordNotifications.SingleJobCompletionText : ListingDiscordNotifications.CompletionText,
                allowedMentions: CompletionMentions(), options: new RequestOptions {
                    CancelToken = cancellation, Timeout = 10000, RetryMode = RetryMode.AlwaysFail
                });
            if (!sent.MentionedEveryone) throw new InvalidOperationException("Discord did not confirm the everyone mention");
        }
    }
}
