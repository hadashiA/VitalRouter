using System.Collections;
using System.Collections.Generic;

namespace VitalRouter;

public interface ICommand
{
}

public class SequenceCommand : ICommand, IReadOnlyList<ICommand>
{
    struct Enumerator : IEnumerator<ICommand>
    {
        readonly SequenceCommand source;
        int currentIndex;

        public Enumerator(SequenceCommand source)
        {
            this.source = source;
            currentIndex = -1;
        }

        public ICommand Current => source[currentIndex];
        object IEnumerator.Current => Current;
        public void Dispose() { }
        public bool MoveNext() => ++currentIndex <= source.Count - 1;

        public void Reset()
        {
            currentIndex = -1;
        }
    }

    readonly List<ICommand> commands = new();
    public IEnumerator<ICommand> GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int Count => commands.Count;

    public ICommand this[int index] => commands[index];

    public void Add(ICommand cmd)
    {
        commands.Add(cmd);
    }
}
