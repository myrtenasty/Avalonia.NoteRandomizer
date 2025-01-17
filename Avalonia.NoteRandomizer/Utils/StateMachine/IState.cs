namespace Avalonia.NoteRandomizer.Utils.StateMachine;

public interface IState
{
    void OnEnter();
    void OnUpdate();
    void OnExit();
}