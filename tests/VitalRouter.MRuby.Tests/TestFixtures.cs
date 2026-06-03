using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChibiRuby.Serializer;
using VitalRouter;

namespace VitalRouter.MRuby.Tests;

[MRubyObject]
public partial struct TestCommand : ICommand
{
    public int Value;
}

[MRubyObject]
public partial struct MoveCommand : ICommand
{
    public string Id;
    public int X;
    public int Y;
}

public enum Facing
{
    North,
    South,
}

[MRubyObject]
public partial struct RichCommand : ICommand
{
    public int IntValue;
    public float FloatValue;
    public bool BoolFlag;
    public string Name;
    public Facing Facing;
    public int[] Numbers;
    public List<string> Tags;
    public Dictionary<string, int> Scores;
    public int? Nullable;

    [MRubyMember("aliased_key")]
    public int Renamed;

    [MRubyIgnore]
    public int Ignored;
}

public class TestCommandRecorder : IAsyncCommandSubscriber
{
    readonly ConcurrentQueue<ICommand> received = new();

    public IReadOnlyList<ICommand> Received => received.ToList();

    public ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
    {
        received.Enqueue(command);
        return default;
    }
}
