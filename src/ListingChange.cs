namespace PartyViewKR;

// A managed request, bound to the listing from which the user opened a popup.
// Native positions, rather than compact display positions, identify job slots.
internal sealed class ListingChange(object edit)
{
    internal object Edit { get; } = edit;
    internal RefreshTarget Original => Edit switch {
        ListingJobEdit e => e.Original, ListingDescriptionEdit e => e.Original,
        ListingObjectiveEdit e => e.Original, ListingBeginnerEdit e => e.Original,
        ListingUniqueJobsEdit e => e.Original, ListingPrivateEdit e => e.Original,
        ListingCompletionEdit e => e.Original, ListingItemLevelEdit e => e.Original,
        ListingRuleEdit e => e.Original, _ => throw new InvalidDataException("unknown_change")
    };
    internal int NativeIndex => Edit is ListingJobEdit e && e.Position > 0 && e.Position <= e.Original.Slots.Slots.Length
        ? e.Original.Slots.Slots[e.Position - 1].NativeIndex : -1;
    internal string Key => Edit switch {
        ListingJobEdit => $"job:{NativeIndex}", ListingRuleEdit e => $"rule:{e.Kind}",
        _ => Edit.GetType().Name
    };
    internal string Title => Edit switch {
        ListingJobEdit e => $"{e.Original.Slots.PositionLabel(e.Original.Slots.Slots[e.Position - 1])}번 자리 허용 직업",
        ListingDescriptionEdit => "소개말", ListingObjectiveEdit => "목적",
        ListingBeginnerEdit => "초행 및 초보자 환영", ListingUniqueJobsEdit => "잡 중복 없음",
        ListingPrivateEdit => "비공개 및 비밀번호", ListingCompletionEdit => "완료조건",
        ListingItemLevelEdit => "평균 아이템 레벨", ListingRuleEdit e => ListingRules.Title(e.Kind), _ => "설정"
    };
    internal string ValueLabel => Edit switch {
        ListingJobEdit e => e.Rules.Describe(e.Selected).Anyone ? "누구나 가능" : $"허용 직업 {e.Rules.Describe(e.Selected).Names.Length}개",
        ListingDescriptionEdit e => e.Value.Length == 0 ? "(빈 소개말)" : e.Value,
        ListingObjectiveEdit e => ListingObjectives.Label(e.Value),
        ListingBeginnerEdit e => e.Enabled ? "사용" : "사용 안 함",
        ListingUniqueJobsEdit e => e.Enabled ? "사용" : "사용 안 함",
        ListingPrivateEdit e => e.Enabled ? "비공개 · 비밀번호 ****" : "공개",
        ListingCompletionEdit e => ListingCompletions.Label(e.Value),
        ListingItemLevelEdit e => ListingItemLevels.Label(e.Value),
        ListingRuleEdit e => ListingRules.Label(e.Kind, e.Value), _ => ""
    };
    internal bool Valid => Edit switch {
        ListingJobEdit e => e.Valid, ListingDescriptionEdit e => e.Valid,
        ListingObjectiveEdit e => e.Valid, ListingBeginnerEdit e => e.Valid,
        ListingUniqueJobsEdit e => e.Valid, ListingPrivateEdit e => e.Valid,
        ListingCompletionEdit e => e.Valid, ListingItemLevelEdit e => e.Valid,
        ListingRuleEdit e => e.Valid, _ => false
    };
    internal bool IsPrivatePassword => Edit is ListingPrivateEdit { Enabled: true };

    internal ListingChange Rebase(RefreshTarget target) => new(Edit switch {
        ListingJobEdit e => e with { Original = target,
            Position = Array.FindIndex(target.Slots.Slots, s => s.NativeIndex == NativeIndex) + 1 },
        ListingDescriptionEdit e => e with { Original = target }, ListingObjectiveEdit e => e with { Original = target },
        ListingBeginnerEdit e => e with { Original = target }, ListingUniqueJobsEdit e => e with { Original = target },
        ListingPrivateEdit e => e with { Original = target }, ListingCompletionEdit e => e with { Original = target },
        ListingItemLevelEdit e => e with { Original = target }, ListingRuleEdit e => e with { Original = target },
        _ => throw new InvalidDataException("unknown_change")
    });

    // Check only this requested field in the fresh server listing. Other failures
    // must not make a successfully applied item appear to have failed as well.
    internal bool MatchesListing(RefreshTarget target) => Edit switch {
        ListingJobEdit e => target.Slots.Slots.FirstOrDefault(s => s.NativeIndex == NativeIndex) is { HasMember: false } slot && e.MatchesSelectedFlags(slot.SlotFlags),
        ListingDescriptionEdit e => e.ValidationError.Length == 0 && ListingIdentity.SameText(e.Value, target.Description),
        ListingObjectiveEdit e => e.Value.HasValue && ListingObjectives.Read(target.Criteria) == e.Value,
        ListingBeginnerEdit e => ListingBeginners.Read(target.Criteria) == e.Enabled,
        ListingUniqueJobsEdit e => ListingUniqueJobs.Read(target.ExtraCriteria) == e.Enabled,
        ListingPrivateEdit e => ListingPrivate.Read(target.ExtraCriteria) == e.Enabled,
        ListingCompletionEdit e => e.Value.HasValue && ListingCompletions.Read(target.Criteria) == e.Value,
        ListingItemLevelEdit e => e.Value.HasValue && ListingItemLevels.Read(target.ExtraCriteria) == e.Value,
        ListingRuleEdit e => ListingRules.ValidValue(e.Kind, e.Value) && ListingRules.Read(target.Criteria, e.Kind) == e.Value,
        _ => false
    };

    internal bool Begin(IListingRefreshHost host, out string error) => Edit switch {
        ListingJobEdit e => host.BeginSlotEdit(Original, e, out error),
        ListingDescriptionEdit e => host.BeginDescriptionEdit(Original, e, out error),
        ListingObjectiveEdit e => host.BeginObjectiveEdit(Original, e, out error),
        ListingBeginnerEdit e => host.BeginBeginnerEdit(Original, e, out error),
        ListingUniqueJobsEdit e => host.BeginUniqueJobsEdit(Original, e, out error),
        ListingPrivateEdit e => host.BeginPrivateEdit(Original, e, out error),
        ListingCompletionEdit e => host.BeginCompletionEdit(Original, e, out error),
        ListingItemLevelEdit e => host.BeginItemLevelEdit(Original, e, out error),
        ListingRuleEdit e => host.BeginRuleEdit(Original, e, out error),
        _ => throw new InvalidDataException("unknown_change")
    };
    internal SlotEditProgress Update(IListingRefreshHost host, out RefreshTarget expected, out string error) => Edit switch {
        ListingJobEdit => host.UpdateSlotEdit(out expected, out error),
        ListingDescriptionEdit => host.UpdateDescriptionEdit(out expected, out error),
        ListingObjectiveEdit => host.UpdateObjectiveEdit(out expected, out error),
        ListingBeginnerEdit => host.UpdateBeginnerEdit(out expected, out error),
        ListingUniqueJobsEdit => host.UpdateUniqueJobsEdit(out expected, out error),
        ListingPrivateEdit => host.UpdatePrivateEdit(out expected, out error),
        ListingCompletionEdit => host.UpdateCompletionEdit(out expected, out error),
        ListingItemLevelEdit => host.UpdateItemLevelEdit(out expected, out error),
        ListingRuleEdit => host.UpdateRuleEdit(out expected, out error),
        _ => throw new InvalidDataException("unknown_change")
    };
    public override string ToString() => Key; // Never stringify a password-bearing request.
}

internal sealed class ListingPendingChanges
{
    private readonly List<ListingChange> items = [];
    internal IReadOnlyList<ListingChange> Items => items;
    internal ListingChange? Find(string key) => items.FirstOrDefault(c => c.Key == key);
    internal bool CanQueue(ListingChange change) => change.Valid || Find(change.Key) != null &&
        !change.IsPrivatePassword && change.MatchesListing(change.Original);
    internal bool Queue(ListingChange change, out string error)
    {
        error = "변경 내용을 다시 확인해 주세요.";
        if (!CanQueue(change)) return false;
        if (items.FirstOrDefault() is { } first && !SameSource(first.Original, change.Original))
        { error = "모집글이 변경되어 대기 목록을 비웠습니다. 현재 모집글에서 다시 선택해 주세요."; items.Clear(); return false; }
        var index = items.FindIndex(c => c.Key == change.Key);
        if (!change.IsPrivatePassword && change.MatchesListing(change.Original))
        { if (index >= 0) items.RemoveAt(index); }
        else if (index >= 0) items[index] = change;
        else items.Add(change);
        error = ""; return true;
    }
    internal ListingChange[] Take() { var result = items.ToArray(); items.Clear(); return result; }
    // Remove only this run's exact entries, never a later replacement or a new item.
    internal void Consume(ListingChange[] submitted) => items.RemoveAll(item => submitted.Any(c => ReferenceEquals(c, item)));
    internal void Clear() => items.Clear();
    internal static bool SameSource(RefreshTarget a, RefreshTarget b) => a.ListingId == b.ListingId &&
        RefreshEvidence.SameRecruitment(a, b) && ListingJobEdit.SameSlots(a.Slots, b.Slots);
}
