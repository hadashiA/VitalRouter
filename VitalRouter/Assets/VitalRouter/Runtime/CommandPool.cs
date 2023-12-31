using System.Collections.Concurrent;

namespace VitalRouter;

static class CommandPool<T> where T : class, IPoolableCommand, new()
{
    static readonly ConcurrentQueue<T> queue = new();

    public static T Rent()
    {
        if (queue.TryDequeue(out var value))
        {
            return value;
        }
        return new T();
    }

    public static void Return(T command)
    {
        queue.Enqueue(command);
    }
}

public interface ICommandPool
{
    T Rent<T>() where T : class, IPoolableCommand, new();
    void Return<T>(T command) where T : class, IPoolableCommand, new();
}

// public class CommandPoolNode<T> where T : class, IPoolableCommand, new()
// {
//     public T Value { get; set; } = default!;
//     public ref CommandPoolNode<T>? NextNode => ref next;
//     CommandPoolNode<T>? next;
// }

// sealed class CommandPool<T> where T : class, IPoolableCommand, new()
// {
//     int gate;
//     CommandPoolNode<T>? root;
//
//     public CommandPoolNode<T> Rent()
//     {
//         while (Interlocked.CompareExchange(ref gate, 1, 0) != 0)
//         {
//             var v = root;
//             if (v is not null)
//             {
//                 ref var nextNode = ref v.NextNode;
//                 root = nextNode;
//                 nextNode = null;
//                 Volatile.Write(ref gate, 0);
//                 return v;
//             }
//             Volatile.Write(ref gate, 0);
//         }
//         return new CommandPoolNode<T>
//         {
//             Value = new T()
//         };
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public bool TryPush(T item)
//     {
//         TRY_AGAIN:
//         if (Interlocked.CompareExchange(ref gate, 1, 0) == 0)
//         {
//             item.NextNode = root;
//             root = item;
//             Volatile.Write(ref gate, 0);
//             return true;
//         }
//         else
//         {
//             goto TRY_AGAIN;
//         }
//     }
// }
