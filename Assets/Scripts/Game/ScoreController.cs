using System;
using UnityEngine;

/// <summary>
/// 점수 컨트롤러. 위키 Notes/게임 콘텐츠 세부 기획(점수).md:7 규격 준수.
/// int 단위, 처치 100점/처형 50점, 처형은 처치 부분집합, 총점=처치+처형.
/// 게임 중 HUD에는 총점만, 게임오버 화면에 5종 내역 표시.
/// HighScore만 PlayerPrefs에 영속, 나머지는 메모리 1판 한정.
/// </summary>
[DisallowMultipleComponent]
public sealed class ScoreController : MonoBehaviour
{
    public const int KillScorePerEnemy = 100;
    public const int ExecutionBonusScore = 50;
    private const string HighScorePrefsKey = "HighScore";

    [Header("영속화")]
    [Tooltip("HighScore를 PlayerPrefs에 저장하는지")]
    [SerializeField]
    private bool persistHighScore = true;

    private int killScore;
    private int executionScore;
    private int killCount;
    private int executionCount;
    private int highScore;
    private bool isFrozen;

    public int KillScore => killScore;
    public int ExecutionScore => executionScore;
    public int KillCount => killCount;
    public int ExecutionCount => executionCount;
    public int TotalScore => killScore + executionScore;
    public int HighScore => highScore;
    public bool IsFrozen => isFrozen;

    public event Action ScoreChanged;
    public event Action HighScoreChanged;

    private void Awake()
    {
        LoadHighScore();
    }

    private void OnEnable()
    {
        // Enemy 사망 이벤트를 중앙에서 구독하려면 ScoreController가 씬에 상주해야 함.
        // EnemyStatController.Died는 개별로 발생하므로, EnemyStateMachine.HandleDied에서 호출하는 대신
        // ScoreController가 직접 구독하지 않고, EnemyStatController 사망 시 외부에서 AddKill을 호출하도록 설계.
        // 여기서는 기존 EnemyStateMachine이 직접 호출하지 않도록, ScoreController가 전역으로 찾아서 구독하는 방식 대신
        // 외부 호출 방식으로 유지한다.
    }

    private void LoadHighScore()
    {
        if (!persistHighScore)
        {
            highScore = 0;
            return;
        }

        highScore = PlayerPrefs.GetInt(HighScorePrefsKey, 0);
    }

    /// <summary>
    /// 적 사망 시 호출. isExecution=true면 처형 직접 처치로 간주.
    /// 처형 충격파 등 주변 사망은 isExecution=false로 호출해야 함.
    /// </summary>
    public void AddKill(bool isExecution)
    {
        if (isFrozen)
        {
            return;
        }

        killScore += KillScorePerEnemy;
        killCount++;

        if (isExecution)
        {
            executionScore += ExecutionBonusScore;
            executionCount++;
        }

        ScoreChanged?.Invoke();
    }

    /// <summary>
    /// 호환용: EnemyDeathReason 기반 호출.
    /// </summary>
    public void AddKill(EnemyDeathReason reason)
    {
        bool isExecution = reason == EnemyDeathReason.Execution;
        AddKill(isExecution);
    }

    public void Freeze()
    {
        isFrozen = true;
    }

    public void Unfreeze()
    {
        isFrozen = false;
    }

    /// <summary>
    /// 게임오버 시 HighScore 커밋. 신기록이면 저장 후 true 반환.
    /// 낮으면 폐기(false) — HighScore 유지.
    /// </summary>
    public bool TryCommitHighScore()
    {
        int total = TotalScore;
        if (total <= highScore)
        {
            return false;
        }

        highScore = total;
        if (persistHighScore)
        {
            PlayerPrefs.SetInt(HighScorePrefsKey, highScore);
            PlayerPrefs.Save();
        }

        HighScoreChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 재시작 시 메모리 점수만 리셋. HighScore는 보존.
    /// </summary>
    public void ResetForNewGame()
    {
        killScore = 0;
        executionScore = 0;
        killCount = 0;
        executionCount = 0;
        isFrozen = false;
        ScoreChanged?.Invoke();
    }

    /// <summary>
    /// 전체 리셋 (테스트용)
    /// </summary>
    public void ResetAllIncludingHighScore()
    {
        ResetForNewGame();
        highScore = 0;
        if (persistHighScore)
        {
            PlayerPrefs.DeleteKey(HighScorePrefsKey);
            PlayerPrefs.Save();
        }
        HighScoreChanged?.Invoke();
    }

    public ScoreSnapshot GetSnapshot()
    {
        return new ScoreSnapshot(killScore, executionScore, killCount, executionCount, TotalScore, highScore);
    }
}

public readonly struct ScoreSnapshot
{
    public readonly int KillScore;
    public readonly int ExecutionScore;
    public readonly int KillCount;
    public readonly int ExecutionCount;
    public readonly int TotalScore;
    public readonly int HighScore;

    public ScoreSnapshot(int killScore, int executionScore, int killCount, int executionCount, int totalScore, int highScore)
    {
        KillScore = killScore;
        ExecutionScore = executionScore;
        KillCount = killCount;
        ExecutionCount = executionCount;
        TotalScore = totalScore;
        HighScore = highScore;
    }
}
