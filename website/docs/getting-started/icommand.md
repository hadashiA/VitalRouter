---
sidebar_label: ICommand
title: ICommand
---

First, define the data types of the event/message you want to dispatch. In VitalRouter this is called "command". 
Any data type that implements `ICommand` will be available as a command, no matter what the struct/class/record is.

For example, the following definitions are all valid.

```cs
public readonly record struct FooCommand(int X, string Y) : ICommand;
```

```cs
public readonly struct FooCommand : ICommand
{
    public int X { get; init; }
    public string Y { get; init; }
}
```

```cs
public class FooCommand : ICommand
{
    public Guid Id { get; init; }
    public Vector3 Destination { get; init; }
}
```

:::tip
 Here we use the init-only property for simplicity. In your Unity project, you may need to add a definition of type `System.Runtime.CompilerServices.IsExternalInit`as a marker.
 However, this is not a requirement.
 You are welcome to define the datatype any way you like.
Modern C# has additional syntax that makes it easy to define such data-oriented types. Using record or record struct is also a good option.
 In fact, even in Unity, you can use the new syntax by specifying `langVersion:11` compiler option or by doing dll separation.  It would be worth considering.
:::
:::note
Command is a data type (without any functionally). You can call it an event, a message, whatever you like. Forget about the traditional OOP "Command pattern" :) This is intended for data-oriented design.
The name "command" is to emphasize that it is a operation that is "published" to your game system entirely. The word is borrowed from CQRS, EventStorming etc.
:::

The ability to identify the type of event being sent by type has many advantages, so VitalRouter enforces this.

One of the advantages of event being a data type is that it is serializable.

```cs
[Serializable]       // < When you want to serialize to a scene or prefab in Unity.
[MessagePackObject]  // < When you want to go through file or network I/O by MessagePack-Csharp.
[YamlObject]         // < When you want to go through configuration files etc by VYaml.
public readonly struct CharacterSpawnCommand : ICommand
{
    public long Id { get; init; }
    public CharacterType Type { get; init; } 
    public Vector3 Position { get; init; }      	
}
```

:::tip For Unity Project
Modern C# has additional syntax that makes it easy to define such data-oriented types. Using record or record struct is also a good option.

In fact, even in Unity, you can use the new syntax by specifying `langVersion:11` compiler option or by doing dll separation.  It would be worth considering.

And here we use the init-only property for simplicity. 
In your Unity project, you may need to add a definition of type `System.Runtime.CompilerServices.IsExternalInit` as a marker.
:::

Next, define the publisher/subscriber for the command.
This can be done in some styles.

1. [Define a class that defines declarative routing.](./declarative-routing-pattern)
2. [Simply register an event handler directly.](./event-handler-pattern)

