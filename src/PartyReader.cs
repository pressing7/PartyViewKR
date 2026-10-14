using System.Text;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace PartyViewKR;

internal sealed record MemberInfo(ulong ContentId, string Name, uint WorldId, string World,
    uint JobId, string Job, int Level, bool IsSelf, bool IsLeader);

internal sealed record PartySnapshot(string Status, string Source, MemberInfo[] Members,
    DateTime? ReadAt, bool IsStale, string Error, string Diagnostic)
{
    internal static PartySnapshot Empty { get; } = new("로그인 대기", "없음", [], null, false, "", "");
}

internal sealed class PartyReader
{
    internal static readonly Guid ExpectedNative = NativeCompatibility.BuildMvid;
    internal static readonly Guid LoadedNative = NativeCompatibility.LoadedMvid;
    private readonly RuntimeRows rows = new();

    internal PartySnapshot Read(PartySnapshot previous)
    {
        if (!Plugin.ClientState.IsLoggedIn) return PartySnapshot.Empty;
        if (!Plugin.PlayerState.IsLoaded || Plugin.Condition[ConditionFlag.BetweenAreas] ||
            Plugin.Condition[ConditionFlag.BetweenAreas51])
            return previous with { Status = "지역 이동 / 캐릭터 정보 대기 — 마지막 조회 결과", IsStale = true };

        rows.BeginRead();
        var members = new List<MemberInfo>();
        var source = "일반 파티";
        var nativeDiagnostic = "nativeRead=skipped; reason=structure mismatch";
        // Run only on the Dalamud framework thread. The loaded native build
        // is checked against the compiled layout contract before reading native memory.
        if (NativeCompatibility.IsCompatible && ReadCrossRealm(members, out nativeDiagnostic))
            source = "서버 초월 파티";
        else
            ReadRegular(members);

        if (members.Count == 0)
        {
            source = "본인 정보";
            var world = RuntimeRows.RowId(RuntimeRows.PlayerWorld, Plugin.PlayerState);
            var job = RuntimeRows.RowId(RuntimeRows.PlayerJob, Plugin.PlayerState);
            members.Add(Create(Plugin.PlayerState.ContentId, Plugin.PlayerState.CharacterName,
                world, job, Plugin.PlayerState.Level, false));
        }

        // CID is the primary key; names are only a fallback for unavailable IDs.
        var unique = members.GroupBy(m => m.ContentId != 0 ? $"cid:{m.ContentId}" : $"name:{m.Name}@{m.WorldId}")
            .Select(g => g.First()).ToArray();
        var status = source == "본인 정보" ? "본인만 조회됨" : "현재 소속 파티";
        var diagnostic = $"plugin={typeof(Plugin).Assembly.GetName().Version}\ndalamud={typeof(Dalamud.Plugin.IDalamudPlugin).Assembly.GetName().Version}\n" +
            $"expectedNative={ExpectedNative}\nloadedNative={LoadedNative}\n" +
            $"nativeCompatible={NativeCompatibility.IsCompatible}\nregularCount={Plugin.PartyList.Length}\n" +
            $"source={source}\ndisplayedCount={unique.Length}\n{nativeDiagnostic}\n{NativeCompatibility.Diagnostic}\nrowLookupError={rows.Error}";
        var warning = !NativeCompatibility.IsCompatible ? "한섭 구조 DLL의 호환 검사를 통과하지 못해 서버 초월 파티 조회를 건너뛰었습니다." : "";
        if (rows.Error.Length > 0) warning += " 서버·직업 이름 변환에 실패한 항목은 번호로 표시합니다.";
        return new(status, source, unique, DateTime.Now, false, warning.Trim(), diagnostic);
    }

    private MemberInfo Create(ulong cid, string name, uint world, uint job, int level, bool leader)
    {
        var self = cid != 0 ? cid == Plugin.PlayerState.ContentId :
            name == Plugin.PlayerState.CharacterName && world == RuntimeRows.RowId(RuntimeRows.PlayerWorld, Plugin.PlayerState);
        return new(cid, name, world, rows.World(world), job, rows.Job(job), level, self, leader);
    }

    private void ReadRegular(List<MemberInfo> result)
    {
        for (var index = 0; index < Math.Min(Plugin.PartyList.Length, 8); index++)
        {
            var member = Plugin.PartyList[index];
            if (member == null || string.IsNullOrWhiteSpace(member.Name.TextValue)) continue;
            result.Add(Create(member.ContentId, member.Name.TextValue,
                RuntimeRows.RowId(RuntimeRows.MemberWorld, member), RuntimeRows.RowId(RuntimeRows.MemberJob, member),
                member.Level, Plugin.PartyList.PartyLeaderIndex == index));
        }
    }

    private unsafe bool ReadCrossRealm(List<MemberInfo> result, out string diagnostic)
    {
        var proxy = InfoProxyCrossRealm.Instance();
        if (proxy == null) { diagnostic = "nativeRead=true; proxy=null"; return false; }
        var groupIndex = proxy->LocalPlayerGroupIndex;
        var groupCount = proxy->GroupCount;
        diagnostic = $"nativeRead=true; crossRealm={proxy->IsCrossRealm}; inCrossRealmParty={proxy->IsInCrossRealmParty}; " +
            $"groupCount={groupCount}; localGroup={groupIndex}";
        if (!(proxy->IsCrossRealm || proxy->IsInCrossRealmParty) || groupCount is < 1 or > 6 || groupIndex >= groupCount)
            return false;

        ref var group = ref proxy->CrossRealmGroups[groupIndex];
        var count = group.GroupMemberCount;
        diagnostic += $"; nativeMemberCount={count}";
        if (count is < 1 or > 8) return false;
        var read = new List<MemberInfo>();
        for (var i = 0; i < count; i++)
        {
            var member = group.GroupMembers[i];
            var buffer = member.Name;
            var end = buffer.IndexOf((byte)0);
            var name = Encoding.UTF8.GetString(end >= 0 ? buffer[..end] : buffer);
            if (string.IsNullOrWhiteSpace(name)) continue;
            read.Add(Create(member.ContentId, name, (ushort)member.HomeWorld,
                member.ClassJobId, member.Level, member.IsPartyLeader));
        }
        // Prevent a stale/other group from being presented as the user's party.
        if (!read.Any(m => m.IsSelf)) { diagnostic += "; accepted=false; reason=self missing"; return false; }
        result.AddRange(read);
        diagnostic += "; accepted=true";
        return true;
    }
}
