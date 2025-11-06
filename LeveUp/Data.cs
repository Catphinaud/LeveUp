using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Quest = Lumina.Excel.Sheets.Quest;

namespace LeveUp;

public static unsafe class Data
{
    public static int[] CurrentLevels;
    public static int[] TargetLevels;
    public static PlayerState* PlayerStateCached;
    public static CalculatorData[] Calculations;
    public static Span<short> PlayerClassJobLeves => PlayerStateCached->ClassJobLevels;
    public static int PlayerJobLevel(int jobIndex) => PlayerStateCached->ClassJobLevels[jobIndex];
    public static int PlayerJobExperience(int jobIndex) => PlayerStateCached->ClassJobExperience[jobIndex];

    public static int ExpToNextLevel(int level) => ParamGrows!.GetRow((ushort)level)!.ExpToNext;

    // csharp
    public static void SetPlayerJobLevel(int jobIndex, int level)
    {
        if (jobIndex < 0 || jobIndex >= Jobs.Length)
            throw new ArgumentOutOfRangeException(nameof(jobIndex), "jobIndex must be between 0 and 7.");

        // clamp to valid level range
        level = Math.Clamp(level, 1, 99);

        if (PlayerStateCached == null)
            throw new InvalidOperationException("PlayerState not initialized. Call Initialize() first.");

        // Update the underlying player state memory using the same offset used when reading current levels
        PlayerStateCached->ClassJobLevels[jobIndex + 7] = (short)level;

        // Update cached arrays
        if (CurrentLevels == null || CurrentLevels.Length != Jobs.Length)
            CurrentLevels = new int[Jobs.Length];
        CurrentLevels[jobIndex] = level;

        if (TargetLevels == null || TargetLevels.Length != Jobs.Length)
            TargetLevels = new int[Jobs.Length];

        // Optional: refresh calculator data for this job
        if (Calculations != null && jobIndex >= 0 && jobIndex < Calculations.Length)
            Calculations[jobIndex] = new CalculatorData(jobIndex);
    }

    public static Dictionary<string, Dictionary<int, (Leve? normal, Leve? large)>> BestLeves = new();
    public static readonly string[] Jobs = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL"];

    public static readonly MapLinkPayload[] GuildReceptionists =
    {
        new(132, 2, 10.8f, 12.1f), new(128, 11, 10.2f, 15f),
        new(128, 11, 10.2f, 15f), new(131, 14, 10.5f, 13.2f),
        new(133, 3, 12.5f, 8.3f), new(131, 14, 13.9f, 13.2f),
        new(131, 73, 8.9f, 13.6f), new(128, 11, 10f, 8f)
    };


    public static Dictionary<string, List<Leve>[]> Leves = new();
    public static Dictionary<(uint jobId, uint itemId), uint> RecipeMap = new();

    public static ExcelSheet<CraftLeve>? CraftLeves;
    public static ExcelSheet<Item>? Items;
    public static ExcelSheet<RecipeLookup>? RecipeLookups;
    public static ExcelSheet<ParamGrow>? ParamGrows;

    public static void Initialize()
    {
        GenerateExcelSheets();
        GenerateDictionaries();
        InitializeCalculatorData();
        PrecomputeBestLeves();
    }

    private static void GenerateExcelSheets()
    {
        CraftLeves = Plugin.DataManager.GetExcelSheet<CraftLeve>()!;
        Items = Plugin.DataManager.GetExcelSheet<Item>()!;
        RecipeLookups = Plugin.DataManager.GetExcelSheet<RecipeLookup>()!;
        ParamGrows = Plugin.DataManager.GetExcelSheet<ParamGrow>()!;
    }

    private static void InitializeCalculatorData()
    {
        PlayerStateCached = PlayerState.Instance();

        var currentLevels = PlayerStateCached->ClassJobLevels;

        CurrentLevels = new int[8];
        for (var i = 0; i < CurrentLevels.Length; i++)
            CurrentLevels[i] = currentLevels[i + 7];

        TargetLevels = new int[8];
        for (var i = 0; i < TargetLevels.Length; i++)
            TargetLevels[i] = PlayerStateCached->ClassJobLevels[i + 7];

        Calculations = new CalculatorData[8];
        for (var i = 0; i < Calculations.Length; i++) Calculations[i] = new CalculatorData(i);
    }

    private static void PrecomputeBestLeves()
    {
        foreach (var job in Jobs)
        {
            // Initialize the dictionary for storing best Leves for each level
            BestLeves.Add(job, new Dictionary<int, (Leve? normal, Leve? large)>());

            // Flatten all Leves into a single list
            var allLeves = Leves[job].SelectMany(l => l).ToList();

            // Group Leves by ClassJobLevel
            var levesByLevel = allLeves.GroupBy(l => (int)l.ClassJobLevel)
                                       .ToDictionary(g => g.Key, g => g.ToList());

            // Initialize variables to store the best Leves found so far
            Leve? bestNormalLeveSoFar = null;
            Leve? bestLargeLeveSoFar = null;

            // Iterate over levels from 1 to 98
            for (var level = 1; level <= 98;)
            {
                // Try to get the Leves for the current level
                if (levesByLevel.TryGetValue(level, out var currentLevelLeves))
                {
                    foreach (var leve in currentLevelLeves)
                    {
                        // Check the AllowanceCost and determine the best normal and large Leves
                        switch (leve.AllowanceCost)
                        {
                            case 1:
                                if (!bestNormalLeveSoFar.HasValue || leve.ExpReward > bestNormalLeveSoFar.Value.ExpReward)
                                    bestNormalLeveSoFar = leve;
                                break;
                            case 10:
                                if (!bestLargeLeveSoFar.HasValue || leve.ExpReward > bestLargeLeveSoFar.Value.ExpReward)
                                    bestLargeLeveSoFar = leve;
                                break;
                        }
                    }
                }

                // Store the best Leve for this level
                BestLeves[job].Add(level, (bestNormalLeveSoFar, bestLargeLeveSoFar));

                // Adjust the level increment based on the pattern described
                switch (level)
                {
                    case 1:
                        level = 5;
                        break;
                    case < 50:
                        level += 5;
                        break;
                    default:
                        level += 2;
                        break;
                }
            }
        }
    }


    private static void GenerateDictionaries()
    {
        foreach (var job in Jobs)
        {
            Leves.Add(job, new List<Leve>[6]);
            for (var i = 0; i < Leves[job].Length; i++) Leves[job][i] = new List<Leve>();
        }

        var leveSheet = Plugin.DataManager.GameData.Excel.GetSheet<Leve>();
        for (uint i = 0; i < leveSheet.Count; i++)
        {
            var leve = leveSheet.GetRow(i);
            var jobId = leve.LeveAssignmentType.Value.RowId;
            if (jobId < 5 || jobId > 12) continue;

            var jobName = leve.ClassJobCategory.Value.Name.ToString();
            if (!Leves.ContainsKey(jobName)) Leves.Add(jobName, []);

            try
            {
                Leves[jobName][ExpansionIndex(leve.ClassJobLevel)].Add(leve);
                var key = GetRecipeMapKey(leve);
                var recipeId = GetRecipeId(jobName, key.itemId);
                RecipeMap.TryAdd(key, recipeId);
            }
            catch (Exception ex)
            {
                Plugin.ChatGui.PrintError(ex.Message);
            }
        }
    }

    private static uint GetRecipeId(string jobName, uint itemId)
    {
        if (RecipeLookups == null) return 0;
        return jobName switch
        {
            "CRP" => RecipeLookups.Where(r => r.CRP.ValueNullable?.ItemResult.RowId == itemId)
                                  .Select(r => r.CRP.ValueNullable?.RowId ?? 0u)
                                  .FirstOrDefault(),
            "LTW" => RecipeLookups.Where(r => r.LTW.ValueNullable?.ItemResult.RowId == itemId)
                                  .Select(r => r.LTW.ValueNullable?.RowId ?? 0u)
                                  .FirstOrDefault(),
            "BSM" => RecipeLookups.Where(r => r.BSM.ValueNullable?.ItemResult.RowId == itemId)
                                  .Select(r => r.BSM.ValueNullable?.RowId ?? 0u)
                                  .FirstOrDefault(),
            "ARM" => RecipeLookups.Where(r => r.ARM.ValueNullable?.ItemResult.RowId == itemId)
                                  .Select(r => r.ARM.ValueNullable?.RowId ?? 0u)
                                  .FirstOrDefault(),
            "CUL" => RecipeLookups.Where(r => r.CUL.ValueNullable?.ItemResult.RowId == itemId)
                                  .Select(r => r.CUL.ValueNullable?.RowId ?? 0u)
                                  .FirstOrDefault(),
            "ALC" => RecipeLookups.Where(r => r.ALC.ValueNullable?.ItemResult.RowId == itemId)
                                  .Select(r => r.ALC.ValueNullable?.RowId ?? 0u)
                                  .FirstOrDefault(),
            "WVR" => RecipeLookups.Where(r => r.WVR.ValueNullable?.ItemResult.RowId == itemId)
                                  .Select(r => r.WVR.ValueNullable?.RowId ?? 0u)
                                  .FirstOrDefault(),
            "GSM" => RecipeLookups.Where(r => r.GSM.ValueNullable?.ItemResult.RowId == itemId)
                                  .Select(r => r.GSM.ValueNullable?.RowId ?? 0u)
                                  .FirstOrDefault(),
            _ => 0u
        };
    }

    public static Item? GetItem(int id)
    {
        return GetItem((uint)id);
    }

    public static Item? GetItem(uint id)
    {
        return Items?.GetRow(id)!;
    }

    private static (uint jobId, uint itemId) GetRecipeMapKey(Leve leve)
    {
        var craft = CraftLeves.GetRow(leve.DataId.RowId);
        return (leve.LeveAssignmentType.Value!.RowId, (uint)craft!.Item.First().RowId);
    }

    public static int ExpansionIndex(ushort level) => Math.Max((level / 10) - 4, 0);
}
