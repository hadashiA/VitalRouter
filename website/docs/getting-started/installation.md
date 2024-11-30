---
sidebar_label: Installation
title: Installation
---

## Unity

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
https://github.com/hadashiA/VitalRouter.git?path=/src/VitalRouter.Unity/Assets/VitalRouter#1.6.0
```

## .NET

THe following NuGet packages are available.

| Package | Latest version |
|:------------ |:----------- |
| VitalRouter | [![NuGet](https://img.shields.io/nuget/v/VitalRouter)](https://www.nuget.org/packages/VitalRouter) | 
| VitalRouter.Extensions.DependencyInjection | [![NuGet](https://img.shields.io/nuget/v/VitalRouter.Extensions.DependencyInjection)](https://www.nuget.org/packages/VitalRouter.Extensions.DependencyInjection) | 

:::note
For Unity, use of the above package is recommended instead of Nuget.
:::
