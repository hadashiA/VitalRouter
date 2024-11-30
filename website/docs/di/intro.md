---
sidebar_label: Introduction of DI
title: Dependency Injection
---

Dependency Injection (DI) is a powerful pattern for managing object lifecycles and dependencies. Many C# application framework can be used with DI.

In VitalRouter, DI is even more useful when used in conjunction with DI to manage state, such as Subscribe/Unsubscribe, and to automatically perform Interceptor dependency resolution.

Currently, the following DI libraries are supported:

- Unity
   - [VContainer](./vcontainer)
- .NET 
    - [Microsoft.Extensions.DependencyInjection](./microsoft-extensions)
