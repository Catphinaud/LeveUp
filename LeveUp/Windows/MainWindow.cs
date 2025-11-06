using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;


namespace LeveUp.Windows;

public class MainWindow : Window, IDisposable
{
    public readonly Vector2 MinSize = new (717,730);
    public readonly Vector2 MinSizeX = new(float.MaxValue, 230);
    public readonly Vector2 ResizeableSize = new(float.MaxValue, float.MaxValue);
    public float ExpandedHeight = 1600;
    public bool Resize;
    
    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow()
        : base("Leve Up!##MainWindow")
    {
        // SetSizeConstraints(MinSizeX);

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = MinSize,
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        LeveListHelper.MainWindow = this;
        TabHelper.MainWindow = this;
        LeveCalculatorHelper.MainWindow = this;
    }

    public void Dispose() { }
    
    public override void Draw()
    {
        // CheckSizeConstraints();
        TabHelper.DrawLeveJobTabs();
        //DrawLeveReplacementPopup();
    }
    
    private void CheckSizeConstraints()
    {
        if (Resize)
        {
            var size = new Vector2(ImGui.GetWindowWidth(), ExpandedHeight);
            ImGui.SetWindowSize(size);
            Resize = false;
        }
        if(LeveListHelper.Opened)
        {
            ExpandedHeight = ImGui.GetWindowHeight();
        }
    }


    public void SetSizeConstraints(Vector2 max)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = MinSize,
            MaximumSize = max
        };
        Resize = true;
    }
    private int GetExpBetweenLevels(int start, int end)
    {
        var expNeeded = 0;
        for (var i = (uint)start; i < end; i++)
            expNeeded += Data.ParamGrows.GetRow(i).ExpToNext;

        if (Data.PlayerJobLevel(7) == start)
            expNeeded -= Data.PlayerJobExperience(7);
        return expNeeded;
    }

}
