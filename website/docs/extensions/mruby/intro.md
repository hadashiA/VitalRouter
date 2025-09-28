---
sidebar_label: Introduction
title: MRuby Scripting
---

![VitalRouter.MRuby](../../assets/diagram_mruby.svg)

It is very powerful if the publishing of commands can be controlled by external data.

For example, when implementing a game scenario, most of the time we do not implement everything in C# scripts. It is common to express large amounts of text data, branching, flag management, etc. in a simple scripting language or data format.

VitalRouter offers an optional package for this purpose before integrating [mruby](https://github.com/mruby/mruby). ([blog](https://medium.com/@hadashiA/vitalrouter-mruby-generic-ruby-scripting-framework-for-unity-d1b2234a5c33) / [blog (Japanease)](https://hadashikick.land/tech/vitalrouter-mruby)

Ruby has very little syntax noise and is excellent at creating DSLs (Domain Specific Languages) that look like natural language.
Its influence can still be seen in modern languages (like Rust and Kotlin, etc).

mruby is a lightweight Ruby interpreter for embedded use, implemented by the original author of Ruby himself.

- mruby can write DSLs that more closely resemble natural language.
- lua has smaller functions and smaller library size. But mruby would be small enough.


For example, you can easily write `cmd` to your liking by adding method and class definitions.

```ruby
# Publish to C# handlers. (Non-blocking) S
# This line is same as `await PublishAsync(new CharacterMoveCommand { Id = "Bob", To = new Vector3(5, 5) })`
cmd :move, id: "Bob", to: [5, 5]

# You can set any variable you want from 
if state[:flag_1]
  cmd :speak, id: "Bob", body: "Flag 1 is on!"
end
```

And, if you want todo so, wrap `cmd` and design a Ruby interface to your liking to make scripting easier.

Each time the `cmd` call is made, VitalRouter publishes an `ICommand`.
The way to subscribe to this is the same as in the usual VitalRouter.

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
 ```

 ```ruby
 # It can be written like this.
 with(:Bob) do
   move [5, 5]
 end 
```

A notable feature is that while C# is executing async/await, the mruby side suspends and waits without blocking the main thread.
Internally, it leverages mruby's `Fiber`, which can be controlled externally to suspend and resume.

This means that you can freely manipulate C# async handlers from mruby scripts.
It should work effectively even in single-threaded environments like Unity WebGL.

### Getting started VitalRouter.MRuby

The mruby extension is a completely separate package.

Starting with version 2, we have migrated from VitalRouter.MRubyCS to [MRubyCS](https://github.com/hadashiA/MRubyCS), a pure C# implementation.

Up to the Version 1 series, it depended on libmruby native bindings and was limited to certain platforms. 
Starting with version 2, any platform where C# runs is supported.


- [VitalRouter.MRuby v2 (pure C# implementations)](./v1)
  - **Recommended**
- [VitalRouter.MRuby v1 (libmruby native binding used)](./v2)
  - **No longer supported**
  - Documentation for v1 remains available, but maintenance is no longer ongoing.
