
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace LeveUp;

public static class ImGuiHelper
{
    public static void CreateTitleBar(string title)
    {
        var windowWidth = ImGui.GetWindowSize().X;
        var textSize = ImGui.CalcTextSize(title).X;
        var textPosX = (windowWidth - textSize) / 2;
        
        ImGui.Dummy(new Vector2(0, 10));
        
        ImGui.SetCursorPosX(textPosX);
        ImGui.Text(title);
        
        ImGui.Dummy(new Vector2(0, 10));
        
        ImGui.Separator();
    }
    
    public static void SetSpacing(int spacing)
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + spacing);
    }

    public static void SetLineSpacing(float spacing)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spacing);
    }
    
    //Based off OtterGui
    public static void DrawTable<T>(string label, IEnumerable<T> data, Action<T> drawRow, ImGuiTableFlags flags = ImGuiTableFlags.None,
                                    params string[] columnTitles)
    {
        if (columnTitles.Length == 0)
            return;

        // Initialize the table with the provided flags
        if (ImGui.BeginTable(label, columnTitles.Length, flags))
        {
            // Setup columns based on columnTitles
            for (int i = 0; i < columnTitles.Length; i++)
            {
                var columnFlags = ImGuiTableColumnFlags.None;

                // Add default sorting to the first column as an example
                if (i == 0)
                    columnFlags |= ImGuiTableColumnFlags.DefaultSort;

                ImGui.TableSetupColumn(columnTitles[i], columnFlags);
            }

            // Finalize the setup with headers
            ImGui.TableHeadersRow();

            // Draw rows based on the provided data and drawRow method
            foreach (var datum in data)
            {
                ImGui.TableNextRow();
                drawRow(datum);
            }

            ImGui.EndTable();
        }
    }
}
