---
sidebar_label: UniTask
title: UniTask Integration
---

If your project uses [UniTask](https://github.com/Cysharp/UniTask), the `VITALROUTER_UNITASK_INTEGRATION` compiler switch will be automatically enabled.

The following features will be enabled.

- Additional optimizations
    - By default, `SemaphoreSlim` is used for locking async methods, but in UniTask mode, `IUniTaskSource`-based implementations are selected.
- UniTask can be used as the return value of the `[Route]` method.

:::note
Although many of the interface signatures in VitalRouter use `ValueTask`,
UniTask has been designed so that conversion to ValueTask is almost zero-cost in v2.5.1 and later.
:::