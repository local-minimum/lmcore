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
}
