using Dalamud.Interface.ImGuiFileDialog;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Route;
using UntarnishedHeart.Execution.Route.Package;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Windows.Components;

namespace UntarnishedHeart.Windows;

internal class RouteEditor() : CollectionEditorWindowBase<Route>($"路线编辑器###{Plugin.PLUGIN_NAME}-RouteEditor")
{
    private static readonly FileDialogManager FileDialogManager = new();

    private static readonly string[] ConflictChoiceLabels = ["覆盖本地", "保留本地", "另存副本"];

    private PackageImportSession? pendingImport;

    protected override string CollectionID => "Route";

    protected override string SelectorLabel => "选择路线:";

    protected override string EmptyCollectionText => "暂无路线";

    protected override string EmptySelectionText => "请选择一条路线进行编辑";

    protected override IList<Route> Items => PluginConfig.Instance().Routes;

    protected override string GetItemName
    (
        Route item
    ) => item.Name;

    protected override Route CreateNewItem() => new() { Name = $"新路线 {Items.Count + 1}" };

    protected override Route? ImportItem() => Route.ImportFromClipboard();

    protected override void ExportItem
    (
        Route item
    ) => item.ExportToClipboard();

    protected override void SaveItems() => PluginConfig.Instance().Save();

    protected override void DrawEditor
    (
        Route item
    ) => RouteEditorPanel.Draw(item);

    public override void Draw()
    {
        base.Draw();
        FileDialogManager.Draw();
        DrawPackageImportModal();
    }

    protected override void DrawExtraToolbarButtons()
    {
        ImGui.SameLine();

        using (var disabled = ImRaii.Disabled(SelectedItem is null))
        {
            if (ImGuiOm.ButtonIcon("ExportPackage", FontAwesomeIcon.BoxOpen, "导出打包", true) && SelectedItem is not null)
                ExportPackage(SelectedItem);
        }

        ImGui.SameLine();

        if (ImGuiOm.ButtonIcon("ImportPackage", FontAwesomeIcon.FileImport, "导入打包", true))
            ImportPackage();
    }

    private void ExportPackage
    (
        Route route
    )
    {
        if (!route.IsValid)
        {
            NotifyHelper.Instance().ChatError("路线无效，无法导出路线包");
            return;
        }

        var presets   = new List<Preset>();
        var presetIDs = new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in route.Steps)
        {
            foreach (var action in step.Actions)
            {
                if (action is not ExecutePresetAction presetAction)
                    continue;

                if (string.IsNullOrWhiteSpace(presetAction.PresetID))
                {
                    NotifyHelper.Instance().ChatError($"路线包导出失败: 步骤“{step.Name}”存在未绑定的预设引用: {presetAction.PresetName}");
                    return;
                }

                var preset = PluginConfig.Instance().Presets.FirstOrDefault
                    (candidate => string.Equals(candidate.ID, presetAction.PresetID, StringComparison.Ordinal));

                if (preset is not { IsValid: true })
                {
                    NotifyHelper.Instance().ChatError($"路线包导出失败: 找不到预设: {presetAction.PresetName}");
                    return;
                }

                if (presetIDs.Add(preset.ID))
                    presets.Add(preset);
            }
        }

        var safeName = string.Concat
        (
            route.Name.Select
            (static character => Path.GetInvalidFileNameChars().Contains(character) ?
                                     '_' :
                                     character
            )
        );
        FileDialogManager.SaveFileDialog
        (
            "导出路线包",
            ".uthpkg",
            $"{safeName}.uthpkg",
            ".uthpkg",
            (confirmed, path) =>
            {
                if (!confirmed)
                    return;

                try
                {
                    UTHPackageIO.Export(route, presets, path);
                    NotifyHelper.Instance().Chat($"路线包已导出: {path}");
                }
                catch (Exception ex)
                {
                    NotifyHelper.Instance().ChatError($"导出路线包失败: {ex.Message}");
                }
            }
        );
    }

    private void ImportPackage() =>
        FileDialogManager.OpenFileDialog
        (
            "导入路线包",
            ".uthpkg",
            (confirmed, path) =>
            {
                if (!confirmed)
                    return;

                try
                {
                    BeginPackageImport(UTHPackageIO.Read(path));
                }
                catch (Exception ex)
                {
                    NotifyHelper.Instance().ChatError($"导入路线包失败: {ex.Message}");
                }
            }
        );

    private void BeginPackageImport
    (
        UTHPackageContent content
    )
    {
        var session = new PackageImportSession(content);
        var presets = PluginConfig.Instance().Presets;

        foreach (var packagePreset in content.Presets)
        {
            var localPreset = presets.FirstOrDefault(candidate => string.Equals(candidate.ID, packagePreset.ID, StringComparison.Ordinal));
            if (localPreset == null || localPreset.Equals(packagePreset))
                continue;

            session.Conflicts.Add(new PresetConflictItem(packagePreset, localPreset));
        }

        if (session.Conflicts.Count == 0)
        {
            ApplyPackageImport(session);
            return;
        }

        pendingImport = session;
        ImGui.OpenPopup("路线包导入冲突");
    }

    private void ApplyPackageImport
    (
        PackageImportSession session
    )
    {
        var config    = PluginConfig.Instance();
        var remap     = new Dictionary<string, string>(StringComparer.Ordinal);
        var added     = 0;
        var overwrote = 0;
        var skipped   = 0;
        var kept      = 0;
        var copied    = 0;

        foreach (var packagePreset in session.Content.Presets)
        {
            var localPreset = config.Presets.FirstOrDefault(candidate => string.Equals(candidate.ID, packagePreset.ID, StringComparison.Ordinal));

            if (localPreset == null)
            {
                config.Presets.Add(packagePreset);
                added++;
                continue;
            }

            if (localPreset.Equals(packagePreset))
            {
                skipped++;
                continue;
            }

            var choiceIndex = session.Conflicts.FirstOrDefault(conflict => ReferenceEquals(conflict.PackagePreset, packagePreset))?.ChoiceIndex ?? 2;

            switch (choiceIndex)
            {
                case 0:
                    config.Presets[config.Presets.FindIndex(candidate => ReferenceEquals(candidate, localPreset))] = packagePreset;
                    overwrote++;
                    break;

                case 1:
                    kept++;
                    break;

                default:
                    var copy = packagePreset.Copy();
                    copy.ID                 = Guid.NewGuid().ToString("D");
                    remap[packagePreset.ID] = copy.ID;
                    config.Presets.Add(copy);
                    copied++;
                    break;
            }
        }

        var finalPackagePresets = new List<Preset>();

        foreach (var packagePreset in session.Content.Presets)
        {
            finalPackagePresets.Add
            (
                remap.TryGetValue(packagePreset.ID, out var newID) ?
                    config.Presets.First(candidate => string.Equals(candidate.ID, newID, StringComparison.Ordinal)) :
                    packagePreset
            );
        }

        PresetReferenceBinder.BindUnboundReferences(session.Content.Route, finalPackagePresets);

        foreach (var step in session.Content.Route.Steps)
        {
            foreach (var action in step.Actions)
            {
                if (action is not ExecutePresetAction presetAction || string.IsNullOrWhiteSpace(presetAction.PresetID))
                    continue;

                if (remap.TryGetValue(presetAction.PresetID, out var newID))
                    presetAction.PresetID = newID;
            }
        }

        var unbound    = PresetReferenceBinder.BindUnboundReferences(session.Content.Route, config.Presets);
        var routeAdded = !config.Routes.Any(existing => existing.Equals(session.Content.Route));
        var missing    = 0;

        foreach (var step in session.Content.Route.Steps)
        {
            foreach (var action in step.Actions)
            {
                if (action is not ExecutePresetAction presetAction || string.IsNullOrWhiteSpace(presetAction.PresetID))
                    continue;

                if (!config.Presets.Any(candidate => string.Equals(candidate.ID, presetAction.PresetID, StringComparison.Ordinal)))
                    missing++;
            }
        }

        if (routeAdded)
        {
            config.Routes.Add(session.Content.Route);
            SelectedIndex = config.Routes.Count - 1;
        }

        config.Save();

        var routeMessage = routeAdded ?
                               $"路线“{session.Content.Route.Name}”已添加" :
                               $"路线“{session.Content.Route.Name}”内容重复，已跳过";
        NotifyHelper.Instance().Chat($"路线包导入完成: 新增预设 {added}、覆盖 {overwrote}、跳过 {skipped}、保留 {kept}、另存副本 {copied}、未绑定引用 {unbound}、缺失预设 {missing}；{routeMessage}");

        if (missing > 0)
            NotifyHelper.Instance().ChatError($"路线“{session.Content.Route.Name}”存在 {missing} 个缺失预设引用，需要在路线编辑器中重新绑定");
    }

    private void DrawPackageImportModal()
    {
        if (pendingImport == null)
            return;

        if (!ImGui.IsPopupOpen("路线包导入冲突"))
            ImGui.OpenPopup("路线包导入冲突");

        if (!ImGui.BeginPopupModal("路线包导入冲突", ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.Text($"路线“{pendingImport.Content.Route.Name}”包含 {pendingImport.Conflicts.Count} 个内容不同的同 ID 预设：");

        for (var i = 0; i < pendingImport.Conflicts.Count; i++)
        {
            var conflict = pendingImport.Conflicts[i];
            ImGui.Text($"{conflict.PackagePreset.Name}（本地: {conflict.LocalPreset.Name}）");
            ImGui.SameLine();
            var choiceIndex = conflict.ChoiceIndex;
            if (ImGui.Combo($"###PackageConflictChoice{i}", ref choiceIndex, ConflictChoiceLabels))
                conflict.ChoiceIndex = choiceIndex;
        }

        if (ImGui.Button("确认导入"))
        {
            ApplyPackageImport(pendingImport);
            pendingImport = null;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();

        if (ImGui.Button("取消"))
        {
            pendingImport = null;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private sealed class PackageImportSession
    (
        UTHPackageContent content
    )
    {
        public UTHPackageContent Content { get; } = content;

        public List<PresetConflictItem> Conflicts { get; } = [];
    }

    private sealed class PresetConflictItem
    (
        Preset packagePreset,
        Preset localPreset
    )
    {
        public Preset PackagePreset { get; } = packagePreset;

        public Preset LocalPreset { get; } = localPreset;

        public int ChoiceIndex { get; set; } = 2;
    }
}
