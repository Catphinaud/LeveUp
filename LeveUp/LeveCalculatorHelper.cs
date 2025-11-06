using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using LeveUp.Windows;
using Lumina.Excel.Sheets;

namespace LeveUp;

public static class LeveCalculatorHelper
{
    public static MainWindow MainWindow;


    private static readonly string[] TableHeaders = ["Name", "Level", "Location", "Exp", "Item", "NQ Attempts", "HQ Attempts"];

    public static void DrawLeveCalculator()
    {
        LeveCalculator();
    }

    private static void LeveCalculator()
    {
        if (Data.PlayerClassJobLeves[TabHelper.JobIndex] == 0)
        {
            if (ImGui.Button("Unlock Job Location"))
                Plugin.GameGui.OpenMapWithMapLink(Data.GuildReceptionists[TabHelper.Index]);
        }
        else
        {
            DisplayCalculations();
        }

        ImGuiHelper.SetSpacing(5);
        ImGui.Separator();
    }

    private static void DisplayCalculations()
    {
        try { DrawHeader(); }
        catch (Exception ex) { Plugin.ChatGui.Print(ex.ToString()); }

        ImGuiHelper.SetSpacing(5);
        if (ImGui.BeginChild("Todo Table", new Vector2(0, 0), false, ImGuiWindowFlags.NoScrollbar))
            DrawTable(Data.Calculations[TabHelper.Index].LeveTable);
        ImGui.EndChild();
        //CreateTable(Data.Calculations[LeveTabHelper.Index].SuggestedLeves, true);
        DrawFooter();
    }

    private static void DrawTable(IEnumerable<(Leve? leve, int nq, int hq)> leves)
    {
        ImGuiHelper.DrawTable(
            label: "",
            data: leves,
            drawRow: DrawLeveRow,
            flags: ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable |
                   ImGuiTableFlags.RowBg | ImGuiTableFlags.Hideable,
            columnTitles: TableHeaders
        );
    }

    private static void DrawLeveRow((Leve? leve, int nq, int hq) leveData)
    {
        var leve = leveData.leve.Value;
        DrawLeveCell(leve.Name.ToString(), leve, true);
        DrawLeveCell(leve.ClassJobLevel.ToString(), leve);
        DrawLeveCell(leve.PlaceNameIssued.Value.Name.ToString(), leve, true);
        DrawLeveCell(leve.ExpReward.ToString("N0"), leve);
        var craft = Data.CraftLeves.GetRow((uint)leve.DataId.RowId);
        var objective = craft.Item.First();
        var itemCount = craft.ItemCount.First() > 1 ? 'x' + craft.ItemCount.First().ToString() : "";
        DrawLeveCell($"{objective.Value.Name} {itemCount}", leve, true, objective.ValueNullable);
        DrawLeveCell(leveData.nq.ToString(), leve);
        DrawLeveCell(leveData.hq.ToString(), leve);
    }

    private static void DrawLeveCell(string content, Leve? leve, bool highlight = false, Item? objective = null)
    {
        ImGui.TableNextColumn();
        ImGui.Text(content);
        var column = ImGui.TableGetColumnIndex();
        if (!highlight) return;

        var itemMin = ImGui.GetItemRectMin();
        var padding = ImGui.GetStyle().CellPadding;

        var min = itemMin - padding;
        var max = ImGui.GetItemRectMax() with { X = itemMin.X + ImGui.GetColumnWidth() } + padding;

        if (ImGui.IsMouseHoveringRect(min, max))
        {
            ImGui.GetWindowDrawList().AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.4f)));
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                ImGui.GetWindowDrawList().AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(new Vector4(7.0f, 7.0f, 7.0f, 0.4f)));
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                ImGui.GetWindowDrawList().AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.4f)));
                switch (column)
                {
                    case 2:
                    {
                        var level = leve.Value.LevelLevemete.Value;
                        var coords = MapUtil.WorldToMap(new Vector3(level.X, level.Y, level.Z), level.Map.Value.OffsetX, level.Map.Value.OffsetY, 0,
                                                        level.Map.Value.SizeFactor);
                        var payload = new MapLinkPayload(level.Territory.Value.RowId, level.Map.Value.RowId, coords.X, coords.Y);
                        Plugin.GameGui.OpenMapWithMapLink(payload);
                        break;
                    }
                    case 4:
                        var recipe = Data.RecipeMap[(leve.Value.LeveAssignmentType.Value.RowId, (uint)objective.Value.RowId)];
                        unsafe
                        {
                            AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipe);
                        }

                        break;
                }
            }
        }
    }

    private static void DrawHeader()
    {
        Data.Calculations[TabHelper.Index].CheckTargetLevel(Data.TargetLevels[TabHelper.Index]);
        ImGui.Text(Data.Calculations[TabHelper.Index].LevelExpLabel);

        ImGui.Text($"{TabHelper.JobName} Leve Calculator [{TabHelper.JobIndex}] [{TabHelper.Index}]");
        ImGui.SameLine();
        ImGuiHelper.SetLineSpacing(5);
        ImGui.Text("Current Level:");
        ImGui.SameLine();
        int currentLevel = Data.PlayerClassJobLeves[TabHelper.JobIndex];
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt($"##CurrentLevelInput{TabHelper.Index}",
                           ref currentLevel))
        {
            currentLevel = Math.Clamp(currentLevel, 1, 100);
            Data.SetPlayerJobLevel(TabHelper.Index, currentLevel);
        }


        ImGui.SameLine();
        ImGuiHelper.SetLineSpacing(5);
        ImGui.Text("Target Level:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("", ref Data.TargetLevels[TabHelper.Index]);
        var addLevel = Data.PlayerClassJobLeves[TabHelper.JobIndex] == 100 ? 0 : 1;
        Data.TargetLevels[TabHelper.Index] = Math.Clamp(Data.TargetLevels[TabHelper.Index], Data.PlayerClassJobLeves[TabHelper.JobIndex] + addLevel, 100);
        ImGui.SameLine();
        ImGuiHelper.SetLineSpacing(ImGui.GetWindowWidth() - 685);
        ImGui.Text(Data.Calculations[TabHelper.Index].TotalExpLabel);
        ImGui.Text("");
    }

    private static void DrawFooter()
    {
        if (ImGui.BeginTable("", 4))
        {
            ImGuiHelper.SetSpacing(10);
            ImGui.TableNextColumn();
            ConfigWindow.DrawAutomatic();

            ImGui.TableNextColumn();
            if (Configuration.Automatic) ConfigWindow.DrawLargeLeves();

            ImGui.TableNextColumn();
            if (Configuration.Automatic) ConfigWindow.DrawSingleLeveMode();

            ImGui.TableNextColumn();
            ImGui.EndTable();
        }
    }
}
