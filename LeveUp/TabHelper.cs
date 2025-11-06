using System.Numerics;
using Dalamud.Bindings.ImGui;
using LeveUp.Windows;

namespace LeveUp;

public static class TabHelper
{
    public static MainWindow MainWindow;
    public static int Index { get; private set; }
    public static int JobIndex => Index + 7;
    public static string JobName => Data.Jobs[Index];

    public static void DrawLeveJobTabs()
    {
        if (ImGui.BeginTabBar("JobBar", ImGuiTabBarFlags.FittingPolicyMask))
        {
            var jobs = Data.Leves.Keys.ToList();
            for(Index = 0; Index < jobs.Count; Index++) CreateTab(jobs[Index]);

            ImGui.EndTabBar();
        }
    }
    
    private static void CreateTab(string job)
    {
        
        if (ImGui.BeginTabItem(job))
        {
            ImGui.BeginChild("TabContent", new Vector2(0, 0), false, ImGuiWindowFlags.NoScrollbar);
          
            LeveCalculatorHelper.DrawLeveCalculator();
            LeveListHelper.DrawLeveTable(job);
            
            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }
}
