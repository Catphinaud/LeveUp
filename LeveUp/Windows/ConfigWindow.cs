using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Lumina.Excel;

namespace LeveUp.Windows;

public class ConfigWindow : Window, IDisposable
{
    private static Configuration Configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("A Wonderful Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(232, 90);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
    }

    public override void Draw()
    {
        DrawAutomatic();
        DrawSingleLeveMode();
        DrawLargeLeves();
    }

    public static void DrawAutomatic()
    {
        var automatic = Configuration.Automatic;
        if (ImGui.Checkbox("Automatic", ref automatic))
        {
            ImGui.BeginTooltip();
            ImGui.Text("Automatically build a leve todo list based on target level");
            ImGui.EndTooltip();
            Configuration.Automatic = automatic;
            Configuration.Save();
        }
    }

    public static void DrawSingleLeveMode()
    {
        var singleLeveMode = Configuration.SingleLeveMode;
        if (ImGui.Checkbox("Single Leve Mode", ref singleLeveMode))
        {
            ImGui.BeginTooltip();
            ImGui.Text("Find the best leve at current level\n(Nice if you want to bulk craft one item)");
            ImGui.EndTooltip();
            Configuration.SingleLeveMode = singleLeveMode;
            Configuration.Save();
        }
    }

    public static void DrawLargeLeves()
    {
        var largeLeves  = Configuration.LargeLeves;
        if (ImGui.Checkbox("Large Leves", ref largeLeves))
        {
            ImGui.BeginTooltip();
            ImGui.Text("Enables Large Leves for Automatic. Large Leves use 10 allowances\n(Not Recommended)");
            ImGui.EndTooltip();
            Configuration.LargeLeves = largeLeves;
            Configuration.Save();
        }       
    }
}
