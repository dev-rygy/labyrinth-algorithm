/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/19/2025
 * Last Modified:   08/14/2025 (Ryan)
 * Notes:           Custom Menu used for demo
*/

using RyansLibrary.Labyrinth;
using UnityEngine;
using UnityEngine.UI;

public class DemoMenuUI : MonoBehaviour
{
    [SerializeField] private Toggle _lootSpawnerToggle;
    [SerializeField] private Toggle _enemySpawnerToggle;

    private LootGenerator _lootGenerator;
    private EnemyGenerator _enemyGenerator;

    private void OnEnable()
    {
        ScenesManager.OnSceneLoaded += UpdateToggles;
        _lootSpawnerToggle.onValueChanged.AddListener(OnLootSpawnerToggleChanged);
        _enemySpawnerToggle.onValueChanged.AddListener(OnEnemySpawnerToggleChanged);
    }

    private void OnDisable()
    {
        ScenesManager.OnSceneLoaded -= UpdateToggles;
        _lootSpawnerToggle.onValueChanged.RemoveListener(OnLootSpawnerToggleChanged);
        _enemySpawnerToggle.onValueChanged.RemoveListener(OnEnemySpawnerToggleChanged);
    }

    private void Start()
    {
        _lootGenerator = LootGenerator.Instance;
        _enemyGenerator = EnemyGenerator.Instance;

        UpdateToggles();
    }

    private void UpdateToggles()
    {
        _lootSpawnerToggle.isOn = _lootGenerator.IsEnabled;
        _enemySpawnerToggle.isOn = _enemyGenerator.IsEnabled;
    }

    private void OnLootSpawnerToggleChanged(bool isOn)
    {
        if (_lootGenerator != null)
        {
            _lootGenerator.ToggleSpawn(isOn);
        }
    }

    private void OnEnemySpawnerToggleChanged(bool isOn)
    {
        if (_enemyGenerator != null)
        {
            _enemyGenerator.ToggleSpawn(isOn);
        }
    }

    public void StartNewGame()
    {
        MapGeneratorController.Instance.ToggleStepwiseDebugging(false);
        ApplicationController.Instance.StartNewGame();
    }

    public void StartDebugging()
    {
        MapGeneratorController.Instance.ToggleStepwiseDebugging(true);
        ApplicationController.Instance.StartNewGame();
    }

    public void ExitDemo()
    {
        Application.Quit();
    }
}
