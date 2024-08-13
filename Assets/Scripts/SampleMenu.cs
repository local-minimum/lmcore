using LMCore.Extensions;
using LMCore.Juice;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.SaveLoad;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SampleMenu : MonoBehaviour
{
    [SerializeField]
    string LevelManagerResoruce = "LevelManager";

    [SerializeField]
    string SaveSystemResource = "SaveSystem";

    [SerializeField]
    string FirstLevelSceneName = "SampleScene";

    [SerializeField]
    GameObject ContinueButton;

    BasicTDSaveSystem saveSystem;
    private void Start()
    {
        saveSystem = BasicTDSaveSystem.InstanceOrResource(SaveSystemResource);
        ContinueButton?.SetActive(saveSystem.HasSave(0));
    }

    public void LoadLevel(bool useAutoSave)
    {
        var continueLoading = TDLevelManager.InstanceOrResource(LevelManagerResoruce).LoadSceneAsync();
        if (useAutoSave == false)
        {
            continueLoading(gameObject.scene, FirstLevelSceneName, null);
        } else
        {
            saveSystem.LoadSaveAsync(
                0,
                (save, invokeLoad) => 
                    continueLoading(
                        gameObject.scene, 
                        save.player.sceneName, 
                        invokeLoad),
                () => continueLoading(gameObject.scene, FirstLevelSceneName, null)
            );
            
        }
    }
}
