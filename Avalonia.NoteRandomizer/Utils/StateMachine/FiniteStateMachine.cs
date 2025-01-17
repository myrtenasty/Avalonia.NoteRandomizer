using System;
using System.Collections.Generic;
using Serilog;

namespace Avalonia.NoteRandomizer.Utils.StateMachine;

public class FiniteStateMachine<TState> where TState : IState
{
    protected Dictionary<Type, TState> states = new();
    protected TState? currentState;
    public TState? CurrentState => currentState;
    public string CurrentStateName => currentState?.GetType().Name ?? string.Empty;
    protected TState? previousState;
    public TState? PreviousState => previousState;
    public string PreviousStateName => previousState?.GetType().Name ?? string.Empty;

    public event Action<TState> OnStateTransition = State => { };
    public string TransitionMessage = string.Empty;

    public void OnUpdate()
    {
        if (states.Count == 0) return;
        currentState?.OnUpdate();
    }

    public virtual void ForceTransitionState<TStateType>(string msg = "") where TStateType : TState
    {
        try
        {
            if (currentState?.GetType() == typeof(TStateType)) return;
            previousState = currentState;

            if (states.TryGetValue(typeof(TStateType), out var state))
            {
                TransitionMessage = msg;
                currentState = state;
                currentState.OnEnter();
                OnStateTransition.Invoke(currentState);
            }
            else
            {
                throw new Exception($"State {typeof(TStateType)} not found");
            }
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to transition state: {e.Message} \n {e.StackTrace}");
        }
    }

    public virtual void TransitionState<TStateType>(string msg = "") where TStateType : TState
    {
        try
        {
            if (currentState?.GetType() == typeof(TStateType)) return;
            previousState = currentState;
            currentState?.OnExit();

            if (states.TryGetValue(typeof(TStateType), out var state))
            {
                TransitionMessage = msg;
                currentState = state;
                currentState.OnEnter();
                OnStateTransition.Invoke(currentState);
            }
            else
            {
                throw new Exception($"State {typeof(TStateType)} not found");
            }
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to transition state: {e.Message} \n {e.StackTrace}");
        }
    }

    public void AddState<TStateType>(TStateType state) where TStateType : TState
    {
        states.Add(state.GetType(), state);
        Log.Information($"Added state: {state.GetType()}, {typeof(TStateType)}");
        if (states.Count == 1)
        {
            TransitionState<TStateType>();
        }
    }
}