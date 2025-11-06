using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace LeveUp;

public class CalculatorData(int index)
{
    // public properties
    public List<(Leve? leve, int nq, int hq)> LeveTable { get; private set; } = [];
    public int TotalNqLeves { get; private set; }
    public int TotalHqLeves { get; private set; }
    
    // private properties
    private int TargetLevel { get; set; }
    private int TotalExpNeeded { get; set; }
    private int CurrentLevelExpCached { get; set; }
    private static bool LargeLeveConfigCached { get; set; }
    private static bool SingleLeveConfigCached { get; set; }
   
    // Read only properties
    private string Job { get; } = Data.Jobs[index];
    private int JobIndex { get; } = index + 7;  // 7 is where CRP starts in param list
    private int NextLevelExpNeeded => Data.ParamGrows!.GetRow((uint)Data.PlayerJobLevel(JobIndex))!.ExpToNext;
    

    public void CheckTargetLevel(int targetLevel)
    {
        if (targetLevel != TargetLevel || CurrentLevelExpCached != Data.PlayerJobExperience(JobIndex) 
                                       || LargeLeveConfigCached != Configuration.LargeLeves || SingleLeveConfigCached != Configuration.SingleLeveMode)
        {
            CurrentLevelExpCached = Data.PlayerJobExperience(JobIndex);
            TargetLevel = targetLevel;

            LargeLeveConfigCached = Configuration.LargeLeves;
            SingleLeveConfigCached = Configuration.SingleLeveMode;
            Calculate();   
        }
    }

    public void Calculate()
    {
        if(Configuration.Automatic) CalculateAutomatic();
    }


    private void CalculateAutomatic()
    {
        LeveTable.Clear();
        TotalNqLeves = 0;
        TotalHqLeves = 0;
        
        if(Configuration.SingleLeveMode) CalculateSingleLeveList();
        else CalculateMultiLeveList();
        
    }

    private void CalculateSingleLeveList()
    {
        var currentLevel = Data.PlayerJobLevel(JobIndex);
        TotalExpNeeded = -Data.PlayerJobExperience(JobIndex);
        
        for (var level = currentLevel; level < TargetLevel; level++)
            TotalExpNeeded += Data.ExpToNextLevel(level);

        var leveTuple = GetLeveTuple(currentLevel, TotalExpNeeded, TotalExpNeeded);
        LeveTable.Add(leveTuple);
    }
    private void CalculateMultiLeveList()
    {
        var currentLevel = Data.PlayerJobLevel(JobIndex);
        
        var nqExpOffset = Data.PlayerJobExperience(JobIndex);
        var hqExpOffset = Data.PlayerJobExperience(JobIndex);
        TotalExpNeeded = -nqExpOffset;
        for (var level = currentLevel; level < TargetLevel; level++)
        {
            var expToNextNq = Data.ExpToNextLevel(level);
            var expToNextHq = expToNextNq;
            TotalExpNeeded += expToNextNq;
            
            expToNextNq -= nqExpOffset;
            expToNextHq -= hqExpOffset;

            var leveTuple = GetLeveTuple(level, expToNextNq, expToNextHq);

            TotalNqLeves += leveTuple.nq;
            TotalHqLeves += leveTuple.hq;

            if (LeveTable.Count > 0 && LeveTable[^1].leve!.Value.DataId.RowId == leveTuple.leve!.Value.DataId.RowId)
            {
                var lastLeve = LeveTable[^1];
                LeveTable[^1] = (lastLeve.leve, lastLeve.nq + leveTuple.nq, lastLeve.hq + leveTuple.hq);
            }
            else LeveTable.Add(leveTuple);

            nqExpOffset = (int)(leveTuple.leve!.Value.ExpReward * leveTuple.nq) - expToNextNq;
            hqExpOffset = (int)(leveTuple.leve.Value.ExpReward * leveTuple.hq * 2) - expToNextHq;
        }
    }

    private (Leve? leve, int nq, int hq) GetLeveTuple(int level, int nqExpNeeded, int hqExpNeeded)
    {
        var levePair = Data.BestLeves[Job][GetLevelKey(level)];
        var selectedLeve = Configuration.LargeLeves && levePair.large != null ? levePair.large : levePair.normal;
            
        var nq = (int)MathF.Ceiling((float)nqExpNeeded / selectedLeve!.Value.ExpReward);
        var hq = (int)MathF.Ceiling((float)hqExpNeeded / (selectedLeve.Value.ExpReward * 2));

        return (selectedLeve, nq, hq);
    }
    
    
    private static int GetLevelKey(int level)
    {
        if (level < 5) return 1;
        return level <= 50
                   ? level / 5 * 5  // For levels <= 50, clamp down to nearest multiple of 5
                   : level / 2 * 2; // For levels > 50, clamp down to even number
    }
    
    public string LevelExpLabel => 
        $"[ Level: {Data.PlayerJobLevel(JobIndex)} ] [ {Data.PlayerJobExperience(JobIndex):N0}/{NextLevelExpNeeded:N0}" +
        $"  ({(float)Data.PlayerJobExperience(JobIndex)/NextLevelExpNeeded:P}%) ]";

    public string TotalExpLabel => $"Total Exp Needed: {Math.Clamp(TotalExpNeeded, 0, float.MaxValue):N0}";
    
    //public string NormalQualityTurnInLabel => $"{Math.Clamp(MathF.Ceiling((float)TotalExpNeeded/SuggestedLeves[0].ExpReward), 0, float.MaxValue):N0}x";
    //public string HighQualityTurnInLabel => $"{Math.Clamp(MathF.Ceiling((float)TotalExpNeeded/SuggestedLeves[0].ExpReward / 2), 0, float.MaxValue):N0}x";
}
