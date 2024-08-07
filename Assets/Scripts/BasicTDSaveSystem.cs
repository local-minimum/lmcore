using LMCore.TiledDungeon.SaveLoad;
using UnityEngine;

/// <summary>
/// Because this demo doesn't need extending it just needs
/// to finalize the types
/// </summary>
public class BasicTDSaveSystem : TDSaveSystem<GameSave> 
{
    [ContextMenu("Log Status")]
    public override void LogStatus()
    {
        base.LogStatus();
    }

    [ContextMenu("AutoSave")]
    void AutoSave()
    {
        Save(
            0,
            () => Debug.Log(PrefixLogMessage("Auto Saved")),
            () => Debug.Log(PrefixLogMessage("Auto Save Failed"))
        );
    }

    [ContextMenu("Load AutoSave")]
    void LoadAutoSave()
    {
        Load(
            0,
            () => Debug.Log(PrefixLogMessage("Loaded Auto Save")),
            () => Debug.Log(PrefixLogMessage("Failed to load Auto Save")));
    }


    [ContextMenu("Wipe All Saves")]
    void Wipe()
    {
        DeleteAllSaves();
    }
}
