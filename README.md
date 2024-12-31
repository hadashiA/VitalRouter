# VitalRouter

[![GitHub license](https://img.shields.io/github/license/hadashiA/VitalRouter)](./LICENSE)
![Unity 2022.2+](https://img.shields.io/badge/unity-2022.2+-000.svg)

VitalRouter, is a source-generator powered zero-allocation fast in-memory messaging library for Unity and .NET.

It can declaratively do async handler/async middleware/sequence control, etc., and could serve as a thin framework to promote unidirectional control flow.

![Diagram](./website/docs/assets/diagram0.svg)

In games, or complex GUI application development, patterns such as central event aggregator/message broker/mediator are powerful patterns to organize N:N relationships.
Assembling an asynchronous function pipeline can be even more effective.

### Features

- Zero allocation message passing
- Thread-safe
- Pub/Sub, Fan-out
- Async / Non-async handlers
- Fast declarative routing pattern
- Naive event handler pattern
- Async interceptor pipelines
    - Parallel, queueing, or other sequential control.
- DI friendly. Also support without DI.
- **Optional Extensions**
    - UniTask support
    - R3 integration
    - MRuby scripting

## Documentation

Visit [vitalrouter.hadashikick.jp](https://vitalrouter.hadashikick.jp) to see the full documentation.

## Installation

### Unity

- **Prerequirements:**
    - Unity 2022.2+
        - This limitation is due to the use of the Incremental Source Generator.
- **Optional**
    - Install UniTask >= 2.5.5
        - If [UniTask](https://github.com/Cysharp/UniTask) is installed, `VITALROUTER_UNITASK_INTEGRATION` flag is turned on and the optimized GC-free code is executed.
        - See [UniTask Integration](../extensions/unitask) section for more details.
    - Install VContainer >= 1.15.1
        - For bringing in DI style, VitalRouter supports Integration with VContainer, a fast and lightweight DI container for Unity.
        - See [DI](../di/vcontainer) section for more details.

Then, add git URL from Package Manager:

```
https://github.com/hadashiA/VitalRouter.git?path=/src/VitalRouter.Unity/Assets/VitalRouter#1.6.2
```

### .NET

THe following NuGet packages are available.

| Package | Latest version |
|:------------ |:----------- |
| VitalRouter | [![NuGet](https://img.shields.io/nuget/v/VitalRouter)](https://www.nuget.org/packages/VitalRouter) | 
| VitalRouter.Extensions.DependencyInjection | [![NuGet](https://img.shields.io/nuget/v/VitalRouter.Extensions.DependencyInjection)](https://www.nuget.org/packages/VitalRouter.Extensions.DependencyInjection) | 

> [!NOTE]
> For Unity, use of the above package is recommended instead of Nuget.


## Async interceptor pipeline

Pipelining of async interceptors for published messages is possible. This is a general strong pattern for data exchange.

<img src="./website/docs/assets/diagram_interceptors.svg" alt="Interceptor Diagram" width="50%" />

[Read more](https://vitalrouter.hadashikick.jp/pipeline/interceptor)

## UniTask Integration

UniTask is a fast async/await extension for Unity. VitalRouter actively supports UniTask.

[Read more](https://vitalrouter.hadashikick.jp/extensions/unitask)

## R3 Integration

R3 is the next generation Reactive Extensions implementation in the C# world. It is an excellent alternative to asynchronous streams, but also an excellent alternative to local events.

VitalRouter supports the ability to work with R3.

[Read more](https://vitalrouter.hadashikick.jp/extensions/r3)

## MRuby scripting?

It is very powerful if the publishing of commands can be controlled by external data.

For example, when implementing a game scenario, most of the time we do not implement everything in C# scripts. It is common to express large amounts of text data, branching, flag management, etc. in a simple scripting language or data format.

VitalRouter offers an optional package for this purpose before integrating [mruby](https://github.com/mruby/mruby). ([blog](https://medium.com/@hadashiA/vitalrouter-mruby-generic-ruby-scripting-framework-for-unity-d1b2234a5c33) / [blog (Japanease)](https://hadashikick.land/tech/vitalrouter-mruby)

Fiber in mruby and async/await in C# are fully integrated.

![MRuby and C# Diagram](./website/docs/assets/diagram_mruby.svg)

[Read more](https://vitalrouter.hadashikick.jp/extensions/mruby)

## LISENCE

MIT

## AUTHOR

@hadashiA
