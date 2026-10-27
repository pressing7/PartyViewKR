using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace PartyViewKR;

public sealed partial class Plugin
{
    private void UpdateChatSending(long now)
    {
        if (discordClient?.Running == true) discordClient.ChatSending.Update(new ChatSendHost(this), now);
    }

    private sealed unsafe class ChatSendHost(Plugin plugin) : IGameChatSendHost
    {
        public ulong SelfId => ClientState.IsLoggedIn && PlayerState.IsLoaded ? PlayerState.ContentId : 0;
        public string? BlockReason => plugin.disposed ? "플러그인이 종료 중입니다." :
            !NativeCompatibility.IsCompatible ? "구조 DLL 호환 검사를 통과하지 못해 채팅 전송을 중단했습니다." :
            Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51] ? "지역 이동 중에는 채팅을 전송할 수 없습니다." : null;

        public bool TrySend(string command, ChatReplyTarget? reply, out string error)
        {
            error = BlockReason ?? "";
            if (error.Length != 0) return false;
            if (SelfId == 0) { error = "게임에 로그인해 주세요."; return false; }
            var ui = UIModule.Instance();
            if (!NativeReadGuard.Readable(ui, 8)) { error = "게임 채팅 입력 기능을 확인하지 못했습니다."; return false; }
            if (reply != null && !ReplyMatches(reply)) {
                error = "게임의 최근 귓속말 대상과 선택한 상대가 일치하지 않아 전송하지 않았습니다. 새 귓속말을 받은 뒤 다시 답장해 주세요.";
                return false;
            }
            var text = Utf8String.FromString(command);
            if (text == null) { error = "채팅 입력을 준비하지 못했습니다."; return false; }
            try {
                text->SanitizeString((AllowedEntities)0x27F);
                if (text->ToString() != command) { error = "게임에서 허용하지 않는 문자가 포함되어 있어 전송하지 않았습니다."; return false; }
                // Preserve the game's own /p, /a, /tell and /r dispatch and
                // cross-world permissions. Never manufacture a TellReason/CID.
                ui->ProcessChatBoxEntry(text);
                return true;
            }
            finally { text->Dtor(true); }
        }

        private bool ReplyMatches(ChatReplyTarget expected)
        {
            var history = AcquaintanceModule.Instance();
            if (!NativeReadGuard.Readable(history, (nuint)sizeof(AcquaintanceModule)) || history->NumTellHistoryEntries is 0 or > 16) return false;
            var latest = history->GetTellHistory(0);
            if (!NativeReadGuard.Readable(latest, (nuint)sizeof(AcquaintanceModule.Acquaintance)) || latest->ContentId == 0 || latest->WorldId == 0 ||
                !NativeReadGuard.Text(&latest->Name, out var name) || name != expected.Name) return false;
            return plugin.chatRows.World(latest->WorldId) == expected.World;
        }
    }
}
