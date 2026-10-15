using System.Collections;
using System.Reflection;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;

namespace PartyViewKR;

// The uploaded KR Dalamud references Lumina 1.0.0.0, whereas the public
// distribution references Lumina 7.0.0.0. Resolve the runtime's public RowRef
// and ExcelSheet APIs instead of compiling against the wrong Lumina assembly.
// No memory offsets, network access, or game-state writes are used here.
internal sealed class RuntimeRows
{
    internal static readonly PropertyInfo MemberWorld = typeof(IPartyMember).GetProperty("World")!;
    internal static readonly PropertyInfo MemberJob = typeof(IPartyMember).GetProperty("ClassJob")!;
    internal static readonly PropertyInfo PlayerWorld = typeof(IPlayerState).GetProperty("HomeWorld")!;
    internal static readonly PropertyInfo PlayerJob = typeof(IPlayerState).GetProperty("ClassJob")!;
    private readonly Dictionary<uint, string> worlds = [];
    private readonly Dictionary<uint, string> jobs = [];
    private object? worldSheet;
    private object? jobSheet;
    private ListingJobRules? recruitmentJobs;
    internal string Error { get; private set; } = "";
    internal void BeginRead() => Error = "";

    internal static uint RowId(PropertyInfo property, object owner)
    {
        var row = property.GetValue(owner) ?? throw new InvalidOperationException($"{property.Name} is null");
        return Convert.ToUInt32(row.GetType().GetProperty("RowId")!.GetValue(row));
    }

    internal string World(uint id) => Lookup(id, MemberWorld, worlds, ref worldSheet, "서버");
    internal string Job(uint id) => id == 0 ? "확인 중" : Lookup(id, MemberJob, jobs, ref jobSheet, "직업");

    internal ListingJobRules RecruitmentJobs()
    {
        if (recruitmentJobs != null) return recruitmentJobs;
        try
        {
            jobSheet ??= GetSheet(MemberJob);
            var rowType = MemberJob.PropertyType.GenericTypeArguments.Single();
            var role = rowType.GetProperty("Role")!;
            var rowId = rowType.GetProperty("RowId")!;
            var name = rowType.GetProperty("Name")!;
            var battleJobs = new List<RecruitmentJob>();
            foreach (var row in (IEnumerable)jobSheet!)
            {
                // Use the game's full battle-job list, not the player's unlocks
                // or CanQueueForDuty (which would omit limited jobs).
                if (Convert.ToByte(role.GetValue(row)) == 0) continue;
                battleJobs.Add(new(Convert.ToUInt32(rowId.GetValue(row)), name.GetValue(row)?.ToString() ?? ""));
            }
            var result = ListingJobRules.Read(battleJobs);
            if (result.Complete) recruitmentJobs = result;
            else Error = "recruitment_job_catalog_incomplete";
            return result;
        }
        catch (Exception ex)
        {
            Error = ex.GetBaseException().Message;
            return ListingJobRules.Unavailable;
        }
    }

    private static object? GetSheet(PropertyInfo property)
    {
        var rowType = property.PropertyType.GenericTypeArguments.Single();
        var getSheet = typeof(IDataManager).GetMethods().Single(m =>
            m.Name == "GetExcelSheet" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
        return getSheet.MakeGenericMethod(rowType).Invoke(Plugin.DataManager, [null, null]);
    }

    private string Lookup(uint id, PropertyInfo property, Dictionary<uint, string> cache, ref object? sheet, string label)
    {
        if (id == 0) return "확인 중";
        if (cache.TryGetValue(id, out var text)) return text;
        try
        {
            sheet ??= GetSheet(property);
            var row = sheet!.GetType().GetMethod("GetRow", [typeof(uint)])!.Invoke(sheet, [id]);
            text = row!.GetType().GetProperty("Name")!.GetValue(row)?.ToString();
            if (!string.IsNullOrWhiteSpace(text)) { cache[id] = text; return text; }
        }
        catch (Exception ex) { Error = ex.GetBaseException().Message; }
        return $"{label} #{id}";
    }
}
