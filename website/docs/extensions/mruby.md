---
sidebar_label: MRuby Scripting 
title: MRuby Scripting
---

![VitalRouter.MRuby](../assets/diagram_mruby.svg)

With VitalRouter, you can use commands to describe events in the game.
At this time, it is very powerful if the publishing of commands can be controlled by external data.

For example, when implementing a game scenario, most of the time we do not implement everything in C# scripts. It is common to express large amounts of text data, branching, flag management, etc. in a simple scripting language or data format.

VitalRouter offers an optional package for this purpose before integrating [mruby](https://github.com/mruby/mruby). ([blog](https://medium.com/@hadashiA/vitalrouter-mruby-generic-ruby-scripting-framework-for-unity-d1b2234a5c33) / [blog (Japanease)](https://hadashikick.land/tech/vitalrouter-mruby)

Ruby has very little syntax noise and is excellent at creating DSLs (Domain Specific Languages) that look like natural language.
Its influence can still be seen in modern languages (like Rust and Kotlin, etc).

mruby is a lightweight Ruby interpreter for embedded use, implemented by the original author of Ruby himself.

- mruby can write DSLs that more closely resemble natural language.
- lua has smaller functions and smaller library size. But mruby would be small enough.
    - The size of mruby minimum shared library less than 700KB.
    - mruby operates with a memory usage of about 100KB.
    - Moreover, it is possible to custom-build `libmruby` as a single library by selecting only the necessary features.

For example, let's prepare the following Ruby script:

```ruby
# Publish to C# handlers. (Non-blocking) Same as `await PublishAsync<CharacterMoveCommand>()`
cmd :move, id: "Bob", to: [5, 5]

# Send log to Unity side (default: UnityEngine.Debug.Log)
log "mruby works"

# You can use any ruby control-flow
3.times do  
  cmd :speak, id: "Bob", text: "Hello Hello Ossu Ossu"

  # Non-blocking waiting. Same as `await UniTask.Delay(TimeSpan.FromSeconds(1))`
  wait 1.sec
end

# You can set any variable you want from Unity.
if state[:flag_1]
  cmd :speak, id: "Bob", body: "Flag 1 is on!"
end
```

Ruby is good at creating DSLs.
For example, you can easily write `cmd` to your liking by adding method and class definitions.

```ruby
 # When the method is defined...
 def move(id, to) = cmd :move, id:, to:
 
 # It can be written like this
 move "Bob", [5, 5]
```

```ruby 
 # Making full use of class definitions and instance_eval...
 class CharacterContext
   def initialize(id)
     @id = id
   end
   
   def move(to)
     cmd :move, id: @id, to: to
   end
 end
 
 def with(id, &block)
   CharacterContext.new(id).instance_eval(block)
 end
 
 # It can be written like this.
 with(:Bob) do
   move [5, 5]
 end 
```


Each time the `cmd` call is made, VitalRouter publishes an `ICommand`.
The way to subscribe to this is the same as in the usual VitalRouter.

A notable feature is that while C# is executing async/await, the mruby side suspends and waits without blocking the main thread.
Internally, it leverages mruby's `Fiber`, which can be controlled externally to suspend and resume.

This means that you can freely manipulate C# async handlers from mruby scripts.
It should work effectively even in single-threaded environments like Unity WebGL.

### Getting started VitalRouter.MRuby

The mruby extension is a completely separate package.
To install it, please add the following URL from the Unity Package Manager.

```
https://github.com/hadashiA/VitalRouter.git?path=/src/VitalRouter.Unity/Assets/VitalRouter.MRuby#1.6.0
```

> [!NOTE]
> Currently VitalRouter.MRuby is only support for Unity.

To execute mruby scripts, first create an `MRubyContext`.

```cs
var context = MRubyContext.Create();
context.Router = Router.Default;                // ... 1
context.CommandPreset = new MyCommandPreset()); // ... 2
```

1. Set the `Router` for VitalRouter. Commands published from mruby are passed to the Router specified here. (Default: Router.Default)
2. The `CommandPreset` is a marker that represents the list of commands you want to publish from mruby. You create it as follows:

```cs
[MRubyCommand("move", typeof(CharacterMoveCommand))]   // < Your custom command name and type list here 
[MRubyCommand("speak", typeof(CharacterSpeakCommand))]
partial class MyCommandPreset : MRubyCommandPreset { }

// Your custom command decralations
[MRubyObject]
partial struct CharacterMoveCommand : ICommand
{
    public string Id;
    public Vector3 To;
}

[MRubyObject]
partial struct CharacterSpeakCommand : ICommand
{
    public string Id;
    public string Text;
}
```

To execute a script with `MRubyContext`, do the following:

```cs
// Your ruby script source here
var rubySource = "cmd :speak, id: 'Bob', text: 'Hello'"

using MRubyScript script = context.CompileScript(rubySource);    
await script.RunAsync();
```

In mruby source, the first argument of `cmd` is any name registered with [MRubyCommand("...")].
The subsequent key/value list represents the member values of the command type (in this case, CharacterSpeakCommand).

> [!TIP]
> The Ruby cmd method waits until the await of the C# async handler completes but does not block the Unity main thread.
> It looks like a normal ruby method, but it's just like a Unity coroutine.
> VitalRouter.MRuby is fully integrated with C#'s async/await.

### `[MRubyObject]`

Types marked with `[MRubyObject]` can be deserialized from the mruby side to the C# side.

- class, struct, and record are all supported.
- A partial declaration is required.
- Members that meet the following conditions are converted from mruby:
    - public fields or properties, or fields or properties with the `[MRubyMember]` attribute.
    - And have a setter (private is acceptable).

```cs
[MRubyObject]
partial struct SerializeExample
{
    // this is serializable members
    public string Id { get; private set; }
    public string Foo { get; init; }
    public Vector3 To;

    [MRubyMember]
    public int Z;
    
    // ignore members
    [MRubyIgnore]
    public float Foo; 
}
```

The list of properties specified by mruby is assigned to the C# member names that match the key names.

Note that the names on the ruby side are converted to CamelCase.
- Example: ruby's `foo_bar` maps to C#'s `FooBar`.


You can change the member name specified from Ruby by using `[MRubyMember("alias name")]`.

```cs
[MRubyObject]
partial class Foo
{
    [MRubyMember("alias_y")]
    public int Y;
} 
```

Also, you can receive data from Ruby via any constructor by using the `[MRubyConstructor]` attribute.

```cs
[MRubyObject]
partial class Foo
{
    public int X { ge; }

    [MRubyConstructor]
    public Foo(int x)
    {
        X = x;
    }
}
```

### Deserialization mruby - C#

`[MRubyObject]` works by deserializing `mrb_value` directly to a C# type.
See the table below for the support status of mutually convertible types.

| mruby                                | C#                                                                                                                                                                                                                                                                                                                                                                                      |
|--------------------------------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Integer`                            | `int`, `uint`, `long`, `ulong`, `shot`, `ushot`, `byte`, `sbyte`, `char`                                                                                                                                                                                                                                                                                                                |
| `Float`                              | `float`, `double`, `decimal`                                                                                                                                                                                                                                                                                                                                                            |
| `Array`                              | `T`, `List<>`, `T[,]`, `T[,]`, `T[,,]`, <br />`Tuple<...>`, `ValueTuple<...>`, <br />, `Stack<>`, `Queue<>`, `LinkedList<>`, `HashSet<>`, `SortedSet<>`, <br />`Collection<>`, `BlockingCollection<>`, <br />`ConcurrentQueue<>`, `ConcurrentStack<>`, `ConcurrentBag<>`, <br />`IEnumerable<>`, `ICollection<>`, `IReadOnlyCollection<>`, <br />`IList<>`, `IReadOnlyList<>`, `ISet<>` |
| `Hash`                               | `Dictionary<,>`, `SortedDictionary<,>`, `ConcurrentDictionary<,>`, <br />`IDictionary<,>`, `IReadOnlyDictionary<,>`                                                                                                                                                                                                                                                                     |
| `String`                             | `string`, `Enum`, `byte[]`                                                                                                                                                                                                                                                                                                                                                              |
| `[Float, Float]`                     | `Vector2`, `Resolution`                                                                                                                                                                                                                                                                                                                                                                 |
| `[Integer, Integer]`                 | `Vector2Int`                                                                                                                                                                                                                                                                                                                                                                            |
| `[Float, Float, Float]`              | `Vector3`                                                                                                                                                                                                                                                                                                                                                                               |
| `[Int, Int, Int]`                    | `Vector3Int`                                                                                                                                                                                                                                                                                                                                                                            |
| `[Float, Float, Float, Float]`       | `Vector4`, `Quaternion`, `Rect`, `Bounds`, `Color`                                                                                                                                                                                                                                                                                                                                      |
| `[Int, Int, Int, Int]`               | `RectInt`, `BoundsInt`, `Color32`                                                                                                                                                                                                                                                                                                                                                       |
| `nil`                                | `T?`, `Nullable<T>`                                                                                                                                                                                                                                                                                                                                                                     |

If you want to customize the formatting behavior, implement `IMrbValueFormatter` .

```csharp
// Example type...
public struct UserId
{
    public int Value;
}
    
public class UserIdFormatter : IMrbValueFormatter<UserId>
{
    public static readonly UserIdFormatter Instance = new();

    public UserId Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
    {
        if (mrbValue.IsNil) return default;
        retun new UserId { Value = mrbValue.IntValue };
    }
}
```

To enable the custom formatter, set MrbValueSerializerOptions as follows.

```csharp
StaticCompositeResolver.Instance
    .AddFormatters(UserIdFormatter.Instance)  // < Yoru custom formatters
    .AddResolvers(StandardResolver.Instance); // < Default behaviours 

// Set serializer options to context.
var context = MRubyContext.Create(...);
context.SerializerOptions = new MrbValueSerializerOptions
{
    Resolver = StaticCompositeResolver
};
```

### MRubyContext

`MRubyContext` provides several APIs for executing mruby scripts.

```cs
using var context = MRubyContext.Create(Router.Default, new MyCommandPreset());

// Evaluate arbitrary ruby script
context.Load(
    "def mymethod(v)\n" +
    "  v * 100\n" +
    "end\n");

// Evaluates any ruby script and returns the deserialized result of the last value.
var result = context.Evaluate<int>("mymethod(7)");
// => 700

// Syntax error and runtime error on the Ruby side can be supplemented with try/catch.
try
{
    context.Evaluate<int>("raise 'ERRRO!'");
}
catch (Exception ex)
{
    // ...
}

// Execute scripts, including the async method including VitalRouter, such as command publishing.
var script = context.CompileScript("3.times { |i| cmd :text, body: \"Hello Hello #{i}\" }");
await script.RunAsync();

// When a syntax error is detected, CompileScript throws an exception.
try
{
    context.CompileScript("invalid invalid invalid");
}
catch (Exception ex)
{
}

// The completed script can be reused.
await script.RunAsync();

// You can supplement Ruby runtime errors by try/catch RunAsync.
try
{
    await script.RunAsync();
}
catch (Exception ex)
{
    // ...
}

script.Dispose();
```

if you want to handle logs sent from the mruby side, do as follows:

```cs
MRubyContext.GlobalLogHandler = message =>
{
    UnityEngine.Debug.Log(messae);
};
```

### Ruby API

The mruby embedded with VitalRouter contains only a portion of the standard library to reduce size.
Please check the [vitalrouter.gembox](https://github.com/hadashiA/VitalRouter/blob/main/src/vitalrouter-mruby/vitalrouter.gembox) to see which mrbgem is enabled.

In addition to the standard mrbgem, the following extension APIs are provided for Unity integration.

```ruby
# Wait for the number of seconds. (Non-blocking)
# It is equivalent to `await UniTask.Delay(TimeSpan.FromSeconds(1))`)
wait 1.0.sec
wait 2.5.secs

# Wait for the number of fames. (Non-blocking)
# It is equivalent to `await UniTask.DelayFrame(1)`) 
wait 1.frame
wait 2.frames 

# Send logs to the Unity side. Default implementation is `UnityEngine.Debug.Log`
log "Log to Unity !"

# Publish VitalRouter command
cmd :your_command_name, prop1: 123, prop2: "bra bra"  
```

> [!NOTE]
>  "Non-blocking" means that after control is transferred to the Unity side, the Ruby script suspends until the C# await completes, without blocking the thread.

### SharedState

Arbitrary variables can be set from the C# side to the mruby side.

```cs
var context = MRubyScript.CreateContext(...);

context.SharedState.Set("int_value", 1234);
context.SharedState.Set("bool_value", true);
context.SharedState.Set("float_value", 678.9);
context.SharedState.Set("string_value", "Hoge Hoge");
context.SharedState.Set("symbol_value", "fuga_fuga", asSymbol: true);
```

SharedState can also be referenced via PublishContext.

```cs
router.Subscribe((cmd, publishContext) =>
{
    // ...
    publishContext.MRubySharedState().Set("x", 1);
    // ...
});

router.SubscribeAwait(async (cmd, publishContext, cancellation) =>
{
    // ...
    publishContext.MRubySharedState().Set("x", 1);
    // ...
});

[Routes]
class MyPresenter
{
    public async UniTask On(FooCommand cmd, PublishContext publishContext)
    {
        publishContext.MRubySharedState().Set("x", 1);
    }
}
```

Shared variables can be referenced from the ruby side as follows.

```ruby
state[:int_value]    #=> 1234
state[:bool_value]   #=> true
state[:float_value]  #=> 678.9
state[:string_value] #=> "Hoge Hoge"
state[:symbol_value] #=> :fuga_fuga

# A somewhat fuzzy matcher, the `is?` method, is available for shared states.
state[:a] #=> 'buz'
state[:a].is?('buz') #=> true
state[:a].is?(:buz)  #=> true
```

### Memory Usage in Ruby

VitalRouter.MRuby specifies Unity's `UnsafeUtility.Malloc` for mruby's memory allocator.

Therefore, mruby's memory usage can be checked from MemoryProfiler, etc.

<img width="910" alt="スクリーンショット 2024-10-04 16 54 23" src="https://github.com/user-attachments/assets/4e29dff0-f2c4-4bef-ae89-d5a7858a3bc3" />


### Supported platforms

VitalRouter.MRuby embeds custom `libmruby` as a Unity native plugin.
It will not work on platforms for which native binaries are not provided.  
Please refer to the following table for current support status.

| Platform    | CPU Arch                | Build              | Tested the actual device                 |
|:------------|-------------------------|--------------------|------------------------------------------|
| Windows     | x64                     | :white_check_mark: | :white_check_mark:                       |
|             | arm64                   |                    |                                          |
| Windows UWP | ?                       | ?                  |                                          |
| macOS       | x64                     | :white_check_mark: | :white_check_mark:                       |
|             | arm64 (apple silicon)   | :white_check_mark: | :white_check_mark:                       |
|             | Universal (x64 + arm64) | :white_check_mark: | :white_check_mark:                       |
| Linux       | x64                     | :white_check_mark: | Tested only the headless Editor (ubuntu) |
|             | arm64                   | :white_check_mark: |                                          |
| iOS         | arm64                   | :white_check_mark: | (planed)                                 |
|             | x64 (Simulator)         | :white_check_mark: | :white_check_mark:                       |
| Android     | arm64                   | :white_check_mark: | :white_check_mark:                       |
|             | x64                     | :white_check_mark: |                                      |
| WebGL       | wasm32                  | :white_check_mark: | :white_check_mark:                       |
| visionOS    | arm64                   | :white_check_mark: |                                          |
|             | x64 (Simulator)         | :white_check_mark: |                                          |

- "Confirmation" means that the author has checked the operation on one or more types of devices. If you have any problems in your environment, please let us know by submitting an issue.
- Build is done in mruby's [build_config.rb](https://github.com/hadashiA/VitalRouter/tree/main/src/vitalrouter-mruby). If you want to add more environments to support, pull requests are welcome.

### How to build VitalRouter.MRuby.Native.(dll|a|so|dylib) ?

VitalRouter.MRuby.Native is simply renamed libmruby.
(It is named for easy identification on crash logs and stack traces.)

The code for the native part of VitalRouter.MRuby is provided as an mruby mrbgem, which is output by the mruby build system into a single libmruby binary.

The steps to build VitalRouter.MRuby.Native.dll are as follows:

1. Clone this repository.
2. Follow the mruby build system and perform the following.
- ```bash
  $ cd VitalRouter/src/vitalrouter-mruby/ext/mruby
  $ ``MRUBY_CONFIG=/path/to/build_config.rb rake`
  ```
- The MRUBY_CONFIG file should be prepared for each target platform.
    - For existing files, they are located in the [./src/vitalrouter-mruby](https://github.com/hadashiA/VitalRouter/tree/main/src/vitalrouter-mruby ) directory
- The mruby rake outputs a static library, but Unity does not support static libraries on some platforms. For this reason, VitalRouter performs conversion to a shared library in an additional task. This is called automatically.
    - https://github.com/hadashiA/VitalRouter/blob/main/src/vitalrouter-mruby/mrbgem.rake#L24
3. Copy the libmruby binary in the ./build/lib directory to unity assets.
- ./src/VitalRouter.Unity/Assets/VitalRouter.MRuby/Runtime/Plugins/

If you want to build VitalRouter.MRuby.Native for a new platform, you should need to create a new build_config file, referring to the existing build_config.*.rb files.

refs:
- https://github.com/mruby/mruby/blob/master/doc/guides/compile.md
- https://github.com/mruby/mruby/tree/master/build_config
