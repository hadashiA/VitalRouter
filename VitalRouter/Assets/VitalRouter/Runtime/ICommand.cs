namespace VitalRouter;

public interface ICommand
{
}

public interface IPoolableCommand : ICommand
{
    void OnReturnToPool();
}

// public class SequenceCommand
// {
// }