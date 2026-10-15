using FFXIVClientStructs.FFXIV.Component.GUI;
using TooltipTree = FFXIVClientStructs.STD.Helper.RedBlackTree<
    FFXIVClientStructs.STD.StdPair<FFXIVClientStructs.Interop.Pointer<FFXIVClientStructs.FFXIV.Component.GUI.AtkResNode>,
        FFXIVClientStructs.Interop.Pointer<FFXIVClientStructs.FFXIV.Component.GUI.AtkTooltipManager.AtkTooltipInfo>>,
    FFXIVClientStructs.Interop.Pointer<FFXIVClientStructs.FFXIV.Component.GUI.AtkResNode>,
    FFXIVClientStructs.STD.Helper.PairKeyExtractor<FFXIVClientStructs.Interop.Pointer<FFXIVClientStructs.FFXIV.Component.GUI.AtkResNode>,
        FFXIVClientStructs.Interop.Pointer<FFXIVClientStructs.FFXIV.Component.GUI.AtkTooltipManager.AtkTooltipInfo>>>;

namespace PartyViewKR;

internal static unsafe class ListingRoleTooltips
{
    // Read only the registered text tooltips owned by this role-selection addon.
    // A bounded traversal validates every tree pointer before following it. It
    // does not hover/click, modify the tooltip map, or read other addons' text.
    internal static bool Read(AtkUnitBase* role, out Dictionary<nuint, string> names, out string diagnostic)
    {
        names = []; diagnostic = "tooltip_manager_unreadable";
        var stage = AtkStage.Instance();
        // AtkStage exceeds the guard's single-read size limit. Validate the base
        // pointer and the particular embedded manager we actually access.
        if (!NativeReadGuard.Readable(stage, (nuint)sizeof(nuint)) ||
            !NativeReadGuard.Readable(&stage->TooltipManager, (nuint)sizeof(AtkTooltipManager))) return false;
        var tree = stage->TooltipManager.TooltipMap.WithOps.Tree;
        if (tree.LongCount is < 0 or > 8192) { diagnostic = "tooltip_count_invalid"; return false; }
        if (tree.LongCount == 0) { diagnostic = "tooltip_map_empty"; return true; }
        if (!NativeReadGuard.Readable(tree.Head, (nuint)sizeof(TooltipTree.Node)) || !tree.Head->_Isnil)
        { diagnostic = "tooltip_head_unreadable"; return false; }
        var pending = new Stack<nuint>();
        var seen = new HashSet<nuint>();
        pending.Push((nuint)tree.Head->_Parent);
        var owned = 0; var unreadable = 0;
        while (pending.TryPop(out var address))
        {
            var node = (TooltipTree.Node*)address;
            if (node == tree.Head) continue;
            if (seen.Count >= tree.LongCount || !seen.Add(address) ||
                !NativeReadGuard.Readable(node, (nuint)sizeof(TooltipTree.Node)) || node->_Isnil)
            { names.Clear(); diagnostic = "tooltip_tree_invalid"; return false; }
            pending.Push((nuint)node->_Left); pending.Push((nuint)node->_Right);
            var info = node->_Myval.Item2.Value;
            if (!NativeReadGuard.Readable(info, (nuint)sizeof(AtkTooltipManager.AtkTooltipInfo)) ||
                info->ParentId != role->Id || info->Type != AtkTooltipType.Text) continue;
            owned++;
            var target = node->_Myval.Item1.Value;
            if (!NativeReadGuard.Readable(target, (nuint)sizeof(AtkResNode)) ||
                !NativeReadGuard.CString(info->AtkTooltipArgs.TextArgs.Text.Value, out var name))
            { unreadable++; continue; }
            if (!names.TryAdd((nuint)target, name))
            { names.Clear(); diagnostic = "tooltip_duplicate_target"; return false; }
        }
        if (seen.Count != tree.LongCount) { names.Clear(); diagnostic = "tooltip_count_mismatch"; return false; }
        diagnostic = $"tooltip_owned={owned}; tooltip_readable={names.Count}; tooltip_unreadable={unreadable}";
        return true;
    }

    // A tooltip may be attached to the component node or to its internal
    // collision/image node. Match the actual node, not the current mouse target.
    internal static string[] ForButton(AtkResNode* owner, AtkComponentButton* button,
        IReadOnlyDictionary<nuint, string> names)
    {
        var targets = new HashSet<nuint> { (nuint)owner };
        ref var uld = ref button->UldManager;
        if (uld.NodeListCount is > 0 and <= 128 &&
            NativeReadGuard.Readable(uld.NodeList, (nuint)(uld.NodeListCount * sizeof(nuint))))
            for (var i = 0; i < uld.NodeListCount; i++) targets.Add((nuint)uld.NodeList[i]);
        targets.Remove(0);
        return names.Where(n => targets.Contains(n.Key)).Select(n => n.Value).Distinct().ToArray();
    }
}
