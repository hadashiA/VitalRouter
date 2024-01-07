VitalRouter, is a zero-allocation message passing tool for Unity (Game Engine).  And the very thin layer that encourages MVP (or whatever)-like design. 
Whether you're an individual developer or part of a larger team, VitalRouter can help you build complex game applications.

Main feature:

| Feature | Description |
| ---- | ---- |
| Declarative routing | The event delivery destination and inetrceptor stack are self-explanatory in the type definition. |
| Async/non-Async handlers | Integrate with async/await (with UniTask), and providing optimized fast pass for non-async way |
| With DI and without DI | Auto-wiring the publisher/subscriber reference by DI (Dependency Injection). But can be used without DI for any project |
| Thread-safe N:N fun-outable, and FIFO  | Built on top of a thread-safe, in-memory, asynchronized  pub/sub system, which is critical in game design.<br><br>Due to the async task's exclusivity control, events are characterized by being consumed in sequence. So it can be used as robust FIFO queue. |

Here's a declarative routing example. Refer to the documentation for more details.

```cs
[Routs]
[Filter(typeof(Logging))]           
[Filter(typeof(ExceptionHandling))] 
[Filter(typeof(GameStateUpdating))] 
public partial class FooPresenter
{
	// Define async event handler
    public async UniTask On(CommandA cmd, CancellationToken cancellation)
    {
	    // Do something after all filters runs on...
	    // 
	    // Here, by awaiting for asynchronous processing, all types of next Command deliveries are waited for.
    }

	// Define non-async event handler
	public void On(CommandB cmd)
	{
		// Do something after all filters runs on...
	}

	// Define handler with extra filter
	[Filter(typeof(ExtraFilter))]
	public async UniTask(CommandC cmd, CancellationToken cancellation)
	{
		// Do something after all filters runs on.
	}
}
```

## Table of Contents


## Installation

Prerequirements:
- Unity 2022.2+
	- This limitation is due to the use of the Incremental Source Generator.
- Install [UniTask](https://github.com/Cysharp/UniTask)
	- Currently, VitalRouter uses UniTask instead of `UnityEngine.Awaitable`. UniTask is a fully featured and fast awaitable implementation.
	- 
	- In a future, if `UnityEngine.Awaitable` is enhanced in a future version of Unity, it may be migrated.
- (optional) Install [VContainer](https://github.com/hadashiA/VContainer) 
	-  For bringing in DI style, VitalRouter supports Integration with VContainer, a fast and lightweight DI container for Unity.

Then, add git URL from Package Manager:

```
https://github.com/hadashiA/Vitalrouter.git?path=/VitalRouter.Unity/Assets/VitalRouter
```

## Getting Started

First, define the data types of the event/message you want to dispatch.
In VitalRouter this is called  "command".

Any data type that implements `ICommand` will be available as a command, no matter what the struct/class/record is.

```cs
public readonly struct FooCommand : ICommand
{
    public int X { get; init; }
    public string Y { get; init; }
}

public readonly record struct BarCommand : ICommand
{
    public Guid Id { get; init; }
    public Vector3 Destination { get; init; }
}
```


Command is a data type (without any functionally). You can call it an event, a message, whatever you like.
The name "command" is to emphasize that it is a operation that is "published" to your game system entirely.  
The reason why the pub/sub model is used is because that command will affect multiple sparse objects.
The word is borrowed from CQRS, DDD's Event storming etc.
the Command object has no methods. This library is intended for data-oriented design. 

Forget about the traditional OOP Command pattern :) 
See [#Technical Explanation](#Recommendation) section to more information.

> [!TIP]
> Here we use the init-only property for simplicity. In your Unity project, you may need to add a definition of type `System.Runtime.CompilerServices.IsExternalInit`as a marker.
> However, this is not a requirement.
> You are welcome to define the datatype any way you like.
 >Modern C# has additional syntax that makes it easy to define such data-oriented types. Using record or record struct is also a good option.
> In fact, even in Unity, you can use the new syntax by specifying `langVersion:11` compiler option or by doing dll separation.  It would be worth considering.

Next, define the class that will handle the commands.

```cs
using VitalRouter;

// Classes with the Routes attribute are the destinations of commands.
[Routes]
public partial class FooPresentor
{
	// This is called when a FooCommand is published.
    public void On(FooCommand cmd)
    {
	    // Do something ...
    }

	// This is called when a FooCommand is published.
	public async UniTask On(BarCommand cmd)
	{
	    // Do something for await ...
	}
}
```
	
Types with the `[Routes]` attribute are analyzed at compile time and a method to subscribe to Command is automatically generated.

Methods that satisfy the following conditions are eligible.
- public accesibility.
-  The argument must be an `ICommand`, or `ICommand` and `CancellationToken`.
-  The return value must be `void`,  or `UniTask`.

For example, all of the following are eligible. Method names can be arbitrary.

```cs
public void On(FooCommand cmd) { /* .. */ }
public async UniTask HandleAsync(FooCommand cmd) { /* .. */ }
public async UniTask Recieve(FooCommand cmd, CancellationToken cancellation) { /* .. */ }

```

> [!NOTE] 
> Is this magic unclear? Well, there are no restrictions by Interface, but it will generate source code that will be resolved at compile time, so you will be able to follow the code well enough.

Now, when and how does the routing defined here call? There are several ways to make it enable this.

### MonoBehaviour based

In a naive Unity project, the easy way is to make MonoBehaviour into Routes.

```cs
[Routes]
public class FooPresenter : MonoBehaviour
{
    void Start()
    {
	    // Start command handling.
	    MapTo(Router.Default); 
    }
}
```

- `MapTo` is an automatically generated instance method.
- When the GameObject is Destroyed, the mapping is automatically removed.

If you publish the command as follows, `FooPresenter` will be invoked.

```cs
await Router.Default.PublishAsync(new FooCommand
{
    X = 111,
    Y = 222,
}
```

If you want to split routing, you may create a Router instance. As follows.

```cs
var anotherRouter = new Router();
```

```cs
MapTo(anotherRouter);
```

```cs
anotherRouter.PublishAsync(..)
```
### DI based

If DI is used, plain C# classes can be used as routing targets.

```cs
[Rouets]
public class FooPresenter // Plain C# class
{
	// ...
}
```

```cs
using VContainer;
using VitalRouter;
using VitalRouter.VContainer;

// VContaner's configuration
public class GameLifetimeSCope : LifetimeScope
{
	protected override void Configure(IContainerBuilder builder)
	{
		builder.RegisterVitalRouter(routing =>
		{
			routing.Map<FooPresenter>();
		});			
	}
}
```

In this case, publisher is also injectable.

```cs
class SomeMyComponent : MonoBehaviour
{
	[SerializeField]
	Button someUIBotton;

	ICommandPublisher publisher;

	[Inject]
    public Construct(ICommandPublisher publisher)
    {
		this.publisher = publisher;
    }

	void Start()
	{
		someUIButton.onClick += ev =>
		{
			publisher.PublishAsync(new FooCommand { X = 1, Y = 2 }).Forget();
		}
	}
}
```

In this case, Just register your Component with the DI container. References are auto-wiring.

```diff
public class GameLifetimeSCope : LifetimeScope
{
	protected override void Configure(IContainerBuilder builder)
	{
+		builder.RegisterComponentInHierarchy<SomeMyComponent>(Lifetime.Singleton);
	
		builder.RegisterVitalRouter(routing =>
		{
			routing.Map<FooPresenter>();
		});			
	}
}

```

> [!NOTE]
> This is a simple demonstration.
> If your codebase is huge, just have the View component notify its own events on the outside, rather than Publish directly. And maybe only the class responsible for the control flow should Publish.

### Manually 

You can also set up your own entrypoint without using `MonoBehaviour` or a DI container.

```cs
var presenter = new FooPresenter();
presenter.MapTo(Router.Default);
```

In this case, unmapping is required manually to discard the FooPresenter.
```cs
presenter.UnmapRoutes();
```

Or, handle subscription.

```cs
var subscription = presenter.MapTo(Router.Default);
subscription.Dispose();
```
## pub/sub control flow

`ICommandPublisher` has an awaitable publish method.

```cs
ICommandPublisher publisher = Router.Default;

await publisher.PublishAsync(command)
await publisher.PublishAsync(command, cancellationToken)
```


> [!IMPORTANT]
>  `PublishAsync` をawaitすると、すべての  Subscribers (`[Routes]` クラス etc)のすべての処理が終わるまでをawaitすることになる。
> 全ての `[Routes]` クラスと Interceptor が処理を終えるまで、次の処理にいかないことに注意してください。


If publish is occured before subscribers have finished consuming, Queue in the order in which Publish is called.
```cs
publisher.PublishAsync(command1).Forget();
publisher.PublishAsync(command2).Forget();
publisher.PublishAsync(command3).Forget();
// ...
```

The following is same for the above.

```cs
publisher.Enqueue(command1);
publisher.Enqueue(command2);
publisher.Enqueue(command3);
// ...
```

`Enqueue` is an alias to `PublishAsync(command).Forget()`.

In other words, per Router, command acts as a FIFO queue for the async task.


Of course, if you do `await`, you can try/catch all subscriber/routes exceptions.

```cs
try
{
	await publisher.PublishAsync(cmd);
}
catch (Exception ex)
{
	// ...
}
```


## Interceptors

Interceptors can intercede additional processing before or after the any published command has been passed and consumed to subscribers.		

![[interceptorstack 2.png]]

### Create a interceptor

Arbitrary interceptors can be created by implementing `ICommandInterceptor`.

Example 1:  Some kind of processing is interspersed before and after the command is consumed.

```cs
class Logging : ICommandInterceptor
{
	public async UniTask InvokeAsync<T>(  
	    T command,  
	    CancellationToken cancellation,  
	    Func<T, CancellationToken, UniTask> next)  
	    where T : ICommand  
	{  
		UnityEngine.Debug.Log($"Start {typeof(T)}");	
		// Execute subsequent routes.	
		await next(command, cancellation);		
		UnityEngine.Debug.Log($"End   {typeof(T)}");
	}		
}
```


Example 2:  try/catch all subscribers exceptions.

```cs
class ExceptionHandling : ICommandInterceptor
{
	public async UniTask InvokeAsync<T>(  
	    T command,  
	    CancellationToken cancellation,  
	    Func<T, CancellationToken, UniTask> next)  
	    where T : ICommand  
	{  
		try
		{
			await next(command, cancellation);
		}
		catch (Exception ex)
		{
			// Error tracking you like			
			UnityEngine.Debug.Log($"oops! {ex.Message}");			
		}
	}		
}
```

Example 3:  Filtering command.

```cs
class MyFilter : ICommandInterceptor
{
	public async UniTask InvokeAsync<T>(  
	    T command,  
	    CancellationToken cancellation,  
	    Func<T, CancellationToken, UniTask> next)  
	    where T : ICommand  
	{  
		if (command is FooCommand { X: > 100 } cmd) 
		{
			// Deny. Skip the rest of the subscribers.
			return;
		}
		// Allow.
		await next(command, cancellation);
	}		
}
```
### Configure interceptors

There are three levels to enable interceptor

1. Apply globally to the `Router`. 
2. Apply it to all methods in the `[Routes]` class.
3. Apply only to specific methods in the `[Routes]` class.

```cs
// Apply to the router.
Router.Default
    .Filter(new Logging());
    .Filter(new ErrorHandling);
```

```cs
// 1. Apply to the router with VContaienr.
builder.RegisterVitalRouter(routing => 
{
	routing.Filters.Add<Logging>();		
	routing.Filters.Add<ErrorHandling>();	
});
```


```cs
// 2. Apply to the type
[Routes]
[Filter(typeof(Logging))]
public partial class FooPresenter
{
	// 3. Apply to the method
	[Filter(typeof(ExtraInterceptor))]
	public void On(CommandA cmd)
	{
		// ...
	}
}
```

All of these are executed in the order in which they are registered, from top to bottom.

If you take the way of 2 or 3, the Interceptor instance is resolved as follows.

- If you are using DI, the DI container will resolve this automatically.
- if you are not using DI, you will need to pass the instance in the `MapTo` call.
 
```cs
MapTo(Router.Default, new Logging(), new ExceptionHandling());
```


## DI scope

VContainer

```cs
public class ParentLifetimeScope : LifetimeScope  
{  
    protected override void Configure(IContainerBuilder builder)  
    {    
        builder.RegisterVitalRouter(router =>  
        {  
            router.Map<PresenterA>();  
        });  
    }  
}
```

## Command Pooling

If Command is struct, VitalRouter avoids boxing, so no heap allocation occurs. This is the reson of using sturct is recommended.

In some cases, however, you may want to use class.
Typically, when Command is treated as a collection element, boxing is unavoidable.

So we support the ability to pooling commands when classes are used.

```cs
public class MyBoxedCommmand : IPoolableCommand
{
	public ResourceA ResourceA { ge; set; }

	void IPoolableCommand.OnReturnToPool()
	{
		ResourceA = null!;
	}
}
```

### Rent from pool

```cs
// To publish, use CommandPool for instantiation.
var cmd = CommandPool<MyBoxedCommand>.Shared.Rent(() => new MyBoxedCommand());

// Lambda expressions are used to instantiate objects that are not in the pool. Any number of arguments can be passed from outside.
var cmd = CommandPool<MyBoxedCommand>.Shared.Rent(arg1 => new MyBoxedCommand(arg1), extraArg);
var cmd = CommandPool<MyBoxedCommand>.Shared.Rent((arg1, arg2) => new MyBoxedCommand(arg1, arg2), extraArg1, extraArg2);
// ...

// Configure value
cmd.ResourceA = resourceA;

// Use it
publisher.PublishAsync(cmd);
```

### Return to pool

```cs
// It is convenient to use the `CommandPooling` Interceptor to return to pool automatically.
Router.Default.Filter(CommandPooling.Instance);


// Or, return to pool manually.
CommandPool<MyBoxedCommand>.Shard.Return(cmd);
```

## Sequence Command

If your command implements `IEnumerable<ICommand>`, it represents a sequence of time series.

```cs
var sequenceCommand = new SequenceCommand
{
	new CommandA(), 
	new CommandB(), 
	new CommandC(),
	// ...
}
```

## Fan-out

It may be too strong a restriction that all commands be executed in series.
If you want to create a group that executes in concurernt, you can creaet a composite router.

```cs
public class CompositeRouter : ICommandPublisher
{
	public Router GroupA { get; } = new();
	public Ruoter GroupB { get; } = new();

	public UniTask PublishAsync<T>(T command, CancellationToken cancellation = default) where T : ICommand
	{
		return UniTask.WhenAll(
			
		);
	}
}
```

## Recommendation, Technical Explanation

Unityは簡単に扱える非常に楽しいゲームエンジンだが、複数のえGame
ゲームは、あるオブジェクトが発火したイベント
つまりこれを素朴に実装してしまうと、

### Unidirectional control flow



 素朴なUnityプログラミングでは、命令を下す側と、命令を下される側が混ぜこぜになりやすい。
Controller/

これがゲームの設計が難しい要因のひとつだ。


M-V-Cはコンポーネントの設計パターンではない。ドメインロジックとプレゼンテーションの分離である。
「Controller」とは、本来は誰からもコントロールされることはない。ゲームシステムが物語の円とりポイントである。
VitalRouter は、誰かがController を宣言するだけだ。だからこのような設計を後押しする。

VitalRouter ではpub/sub モデルを採用した。
また、 Unityのオブジェクトは、実行時にめまぐるしく生成/破棄が繰り返される
あるオブジェクトが発火したイベントが、UI、 非常にたくさんの N:N の関係性を 

 GameObject 同士の メッセージのやりとりのサポートが微妙
 素朴に考えると、GameObject同士の関連が爆発してしまう

双方向バインディングはこの方法に無力である。
昨今、多くのGUIデザインは、単方向フローをとる。

ゲームでは、view
多く

- Component それ自身のUpdate は自身の内側に隠蔽する
- Componentはそれじし


データオリエンテッドデザイン

Command にはメソッドがない。これはただのデータである。
設計上の利点のひとつは、、


隠蔽しなくてよい。


イベントの種類ごとに型を与えることの重要な利点は、シリアライズ可能なことです。
たとえば、
コマンドを順番どおりに保存しておくだけで、あとからそれをゲームのリプレイができます。
また、ネットワークをまたいだ命令や、
エディタいよって Commandのシーケンスを保存しておけば

推奨するデザインについては、詳しくはフィロソフィーセクションを参照してほしい。
しかしもちろん、あなたの


## LISENCE

MIT

## AUTHOR

@hadashiA
