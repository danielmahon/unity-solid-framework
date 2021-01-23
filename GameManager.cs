using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager> {

  public enum GameState { PREGAME, RUNNING, PAUSED }

  public GameObject[] systemPrefabs;
  public event Action<GameManager.GameState, GameManager.GameState> OnGameStateChanged;

  List<GameObject> instancedSystemPrefabs = new List<GameObject>();
  List<AsyncOperation> loadOperations = new List<AsyncOperation>();
  GameState currentGameState = GameState.PREGAME;
  string currentLevelName;

  public GameState CurrentGameState {
    get => currentGameState;
    private set => currentGameState = value;
  }

  void OnEnable() {
    DontDestroyOnLoad(gameObject);
    InstantiateSystemPrefabs();

    UIManager.Instance.OnMainMenuFadeComplete += HandleMainMenuFadeComplete;
  }

  void OnDisable() {
    UIManager.Instance.OnMainMenuFadeComplete -= HandleMainMenuFadeComplete;
  }

  void Update() {
    if (currentGameState == GameState.PREGAME) return;

    if (Input.GetKeyDown(KeyCode.Escape)) {
      TogglePause();
    }
  }

  protected override void OnDestroy() {
    base.OnDestroy();
    foreach (var prefab in instancedSystemPrefabs) {
      Destroy(prefab);
    }
    instancedSystemPrefabs.Clear();
  }

  void OnLoadOperationComplete(AsyncOperation ao) {
    if (loadOperations.Contains(ao)) {
      loadOperations.Remove(ao);
      if (loadOperations.Count == 0) {
        UpdateState(GameState.RUNNING);
      }
    }

    Debug.Log("Load Complete.");
  }

  void UpdateState(GameState state) {
    var previousGameState = currentGameState;
    currentGameState = state;

    switch (currentGameState) {
      case GameState.PREGAME:
        Time.timeScale = 1f;
        break;
      case GameState.RUNNING:
        Time.timeScale = 1f;
        break;
      case GameState.PAUSED:
        Time.timeScale = 0f;
        break;
      default:
        break;
    }

    OnGameStateChanged?.Invoke(currentGameState, previousGameState);

  }

  void OnUnloadOperationComplete(AsyncOperation ao) {
    Debug.Log("Unload Complete.");
  }

  void HandleMainMenuFadeComplete(bool fadeOut) {
    Debug.Log("HandleMainMenuFadeComplete");
    if (!fadeOut && currentLevelName != null) {
      UnloadLevel(currentLevelName);
    }
  }

  void InstantiateSystemPrefabs() {
    GameObject prefabInstance;
    foreach (var prefab in systemPrefabs) {
      prefabInstance = Instantiate(prefab);
      instancedSystemPrefabs.Add(prefabInstance);
    }
  }

  public void LoadLevel(string levelName) {
    var ao = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
    if (ao == null) {
      Debug.LogError($"[GameManager] Unable to load level {levelName}");
      return;
    }
    ao.completed += OnLoadOperationComplete;
    loadOperations.Add(ao);

    currentLevelName = levelName;
  }

  public void UnloadLevel(string levelName) {
    var ao = SceneManager.UnloadSceneAsync(levelName);
    if (ao == null) {
      Debug.LogError($"[GameManager] Unable to unload level {levelName}");
      return;
    }
    ao.completed += OnUnloadOperationComplete;
    currentLevelName = null;
  }

  public void StartGame() {
    LoadLevel("Main");
  }

  public void TogglePause() {
    UpdateState(currentGameState == GameState.RUNNING ? GameState.PAUSED : GameState.RUNNING);
  }

  public void QuitGame() {
    // implement features for quitting
    if (Application.isEditor) {
      UnityEditor.EditorApplication.isPlaying = false;
    } else {
      Application.Quit();

    }
  }

  public void RestartGame() {
    UpdateState(GameState.PREGAME);
  }

}
