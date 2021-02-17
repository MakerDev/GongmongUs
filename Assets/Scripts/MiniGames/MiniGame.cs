using Assets.Scripts;
using cakeslice;
using Cysharp.Threading.Tasks.Triggers;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGameResult
{
    public MiniGame MiniGame { get; set; }
    public bool Passed { get; set; } = false;
}

/// <summary>
/// Base class for all minigames
/// </summary>
public abstract class MiniGame : MonoBehaviour
{
    public static event Action OnStartMiniGame;
    public static event Action<MiniGameResult> OnTurnOffMiniGame;
    public static bool IsPlaying { get; private set; } = false;

    public event Action<MiniGameResult> OnGameCompleted;

    public bool IsCompleted { get; private set; } = false;
    protected MiniGameResult MiniGameResult = new MiniGameResult();

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Player AssignedPlayer { get; private set; } = null;

    [SerializeField]
    private Outline _outline;

    private void Awake()
    {
        MiniGameResult.MiniGame = this;
    }

    public void AssignPlayer(Player player)
    {
        AssignedPlayer = player;
    }

    public void OnEnable()
    {
        OnStartMiniGame?.Invoke();
        MiniGameResult = new MiniGameResult();
        StartMiniGame();
        GameManager.Instance?.DisablePlayerControl();
    }

    public void OnDisable()
    {
        OnTurnOffMiniGame?.Invoke(MiniGameResult);
        OnGameCompleted?.Invoke(MiniGameResult);
        GameManager.Instance?.EnablePlayerControl();
    }

    public virtual void ResetGame() { }

    public void StartMiniGame()
    {
        this.gameObject.SetActive(true);
        IsPlaying = true;
    }

    private void TurnOffGame()
    {
        this.gameObject.SetActive(false);
        IsPlaying = false;
        ResetGame();
    }

    public void CancelMiniGame()
    {
        MiniGameResult.Passed = false;
        TurnOffGame();
    }

    public void CompleteMiniGame(MiniGameResult miniGameResult)
    {
        MiniGameResult = miniGameResult;
        TurnOffGame();

        if (MiniGameResult.Passed)
        {
            AssignedPlayer.OnCompleteMission(miniGameResult);
            IsCompleted = true;

            if (_outline != null)
            {
                _outline.enabled = false;
            }
        }
    }
}
