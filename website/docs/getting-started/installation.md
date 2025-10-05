---
sidebar_label: Installation
title: Installation
---

## Installation

> [!NOTE]
> Starting with version 2.0, distribution in Unity has been changed to NuGet.
> For documentation prior to version 1.x, please refer to [v1](https://github.com/hadashiA/VitalRouter/tree/v1) branch.

The following NuGet packages are available.

| Package                                    | Latest version                                                                                                                                                   |
|:-------------------------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| VitalRouter                                | [![NuGet](https://img.shields.io/nuget/v/VitalRouter)](https://www.nuget.org/packages/VitalRouter)                                                               | 
| VitalRouter.Extensions.DependencyInjection | [![NuGet](https://img.shields.io/nuget/v/VitalRouter.Extensions.DependencyInjection)](https://www.nuget.org/packages/VitalRouter.Extensions.DependencyInjection) | 
| VitalRouter.R3                             | [![NuGet](https://img.shields.io/nuget/v/VitalRouter.R3)](https://www.nuget.org/packages/VitalRouter.R3)                                                         |
| VitalRouter.MRuby                          | [![NuGet](https://img.shields.io/nuget/v/VitalRouter.MRuby)](https://www.nuget.org/packages/VitalRouter.MRuby)                                                   |

### Unity

> [!NOTE]
> Requirements: Unity 20222.2+
> This limitation is due to the use of the Incremental Source Generator.

1. Install [NugetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).
2. Open the NuGet window by going to NuGet > Manage NuGet Packages, after search for the "VitalRouter" packages, and install it.
3. **Optional**
    - The following extensions for Unity are available from the Unity Package Manager:
        - ```
          https://github.com/hadashiA/VitalRouter.git?path=/src/VitalRouter.Unity/Assets/VitalRouter#2.0.0
          ```
        - Install UniTask >= 2.5.5
            - If [UniTask](https://github.com/Cysharp/UniTask) is installed, `VITALROUTER_UNITASK_INTEGRATION` flag is turned on and the optimized GC-free code is executed.
            - See [UniTask Integration](/extensions/unitask.md) section for more details.
        - Install VContainer >= 1.16.6
            - For bringing in DI style, VitalRouter supports Integration with VContainer, a fast and lightweight DI container for Unity.
            - See [DI](/di/vcontainer.md) section for more details.

