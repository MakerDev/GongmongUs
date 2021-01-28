using Cysharp.Threading.Tasks.Triggers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGameResult
{
    public bool Passed { get; set; } = false;
}

/// <summary>
/// Base class for all minigames
/// </summary>
public class MiniGame : MonoBehaviour
{
    public static event Action OnStartMiniGame;
    public static event Action<MiniGameResult> OnCompletedMiniGame;
    public static bool IsPlaying { get; private set; } = false;

    public event Action<MiniGameResult> OnLocalGameCompleted;

    public bool IsCompleted { get; private set; } = false;
    protected MiniGameResult MiniGameResult = new MiniGameResult();

    public void OnEnable()
    {
        OnStartMiniGame?.Invoke();
        MiniGameResult = new MiniGameResult();
        StartMiniGame();
    }

    public void OnDisable()
    {
        OnCompletedMiniGame?.Invoke(MiniGameResult);
        OnLocalGameCompleted?.Invoke(MiniGameResult);
    }

    public void StartMiniGame()
    {
        this.gameObject.SetActive(true);
        IsPlaying = true;
    }

    public void CancelMiniGame()
    {
        MiniGameResult.Passed = false;
        this.gameObject.SetActive(false);
        IsPlaying = false;
    }

    public void CompleteMiniGame(MiniGameResult miniGameResult)
    {
        MiniGameResult = miniGameResult;
        //TODO : report to the GameManaget
        this.gameObject.SetActive(false);
        IsCompleted = true;
        IsPlaying = false;
    }
}
