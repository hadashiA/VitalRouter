using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MRubyCS;
using MRubyCS.Serializer;

namespace VitalRouter.MRuby
{
public delegate ValueTask MRubyPublishDelegate(
    ICommandPublisher publisher,
    MRubyState mruby,
    RHash props,
    CancellationToken cancellation);

public static class MRubyStateExtensions
{
    public static void AddVitalRouter(this MRubyState mrb, Action<MRubyState> configure)
    {
        if (!mrb.TryGetConst(mrb.Intern("VitalRouter"u8), out _))
        {
            mrb.DefineVitalRouter();
        }
        configure(mrb);
    }

    public static void AddCommand<TCommand>(this MRubyState state, string name) where TCommand : ICommand
    {
        var commandTable = state.GetCommandTable();
        var key = state.Intern(name);
        commandTable[key] = async (publisher, s, props, cancellation) =>
        {
            var cmd = MRubyValueSerializer.Deserialize<TCommand>(props, s)!;
            await publisher.PublishAsync(cmd, cancellation);
        };
    }

    public static MRubySharedVariableTable GetSharedVariables(this MRubyState mrb)
    {
        if (!mrb.TryGetConst(mrb.Intern("VitalRouter"u8), out _))
        {
            DefineVitalRouter(mrb);
        }
        var instance = mrb.Send(mrb.ObjectClass, mrb.Intern("state"u8));
        var tableVariable = mrb.GetInstanceVariable(instance.As<RObject>(), mrb.Intern("@table"u8));
        var data = tableVariable.As<RData>().Data;
        return (MRubySharedVariableTable)data;
    }

    static Dictionary<Symbol, MRubyPublishDelegate> GetCommandTable(this MRubyState mrb)
    {
        if (!mrb.TryGetConst(mrb.Intern("VitalRouter"u8), out _))
        {
            mrb.DefineVitalRouter();
        }

        var methodTableValue = mrb.GetConst(mrb.Intern("PUBLISH_METHOD_TABLE"u8), mrb.ObjectClass);
        return (methodTableValue.As<RData>().Data as Dictionary<Symbol, MRubyPublishDelegate>)!;
    }

    public static async ValueTask ExecuteAsync(
        this MRubyState mrb,
        Router router,
        Irep irep,
        CancellationToken cancellation = default)
    {
        var proc = mrb.CreateProc(irep);
        var fiber = mrb.CreateFiber(proc);

        if (router.HasInterceptor<MRubyStateInterceptor>())
        {
            throw new MRubyRoutingException("duplicate script running in same router");
        }
        var interceptor = new MRubyStateInterceptor(mrb);
        router.AddFilter(interceptor);
        try
        {
            using var script = new MRubyRoutingScript(fiber, router);
            await script.RunAsync(cancellation);
        }
        finally
        {
            router.RemoveFilter(x => x is MRubyStateInterceptor);
        }
    }

    public static void DefineVitalRouter(this MRubyState mrb)
    {
        var module = mrb.DefineModule(mrb.Intern("VitalRouter"u8), mrb.ObjectClass, module =>
        {
            var methodTable = new Dictionary<Symbol, MRubyPublishDelegate>();
            module.DefineConst(mrb.Intern("PUBLISH_METHOD_TABLE"u8), new RData(methodTable));

            var sharedStateClass = module.DefineClass(mrb.Intern("SharedState"u8), sharedStateClass =>
            {
                sharedStateClass.DefineMethod(mrb.Intern("initialize"u8), (s, self) =>
                {
                    var table = new MRubySharedVariableTable(mrb);
                    var data = new RData(table);
                    s.SetInstanceVariable(self.As<RObject>(), s.Intern("@table"u8), data);
                    return self;
                });

                sharedStateClass.DefineMethod(mrb.Intern("[]"u8), (s, self) =>
                {
                    var key = s.Intern(s.Stringify(s.GetArgumentAt(0)));
                    var instance = mrb.Send(mrb.ObjectClass, mrb.Intern("state"u8));
                    var tableVariable = mrb.GetInstanceVariable(instance.As<RObject>(), mrb.Intern("@table"u8));
                    var table = (MRubySharedVariableTable)tableVariable.As<RData>().Data;
                    return table.GetOrNil(key);
                });

                sharedStateClass.DefineMethod(mrb.Intern("[]="u8), (s, self) =>
                {
                    var key = s.Intern(s.Stringify(s.GetArgumentAt(0)));
                    var value = s.GetArgumentAt(1);

                    var instance = mrb.Send(mrb.ObjectClass, mrb.Intern("state"u8));
                    var tableVariable = mrb.GetInstanceVariable(instance.As<RObject>(), mrb.Intern("@table"u8));
                    var table = (MRubySharedVariableTable)tableVariable.As<RData>().Data;
                    table.Set(key, value);
                    return value;
                });
            });

            module.DefineMethod(mrb.Intern("log"u8), (s, self) =>
            {
                var message = s.Stringify(s.GetArgumentAt(0));
                UnityEngine.Debug.Log(message);
                return MRubyValue.Nil;
            });

            module.DefineMethod(mrb.Intern("state"u8), (s, self) =>
            {
                var value = s.GetInstanceVariable(self.As<RObject>(), s.Intern("@state"u8));
                if (value.IsNil)
                {
                    value = s.Send(sharedStateClass, s.Intern("new"u8));
                    s.SetInstanceVariable(self.As<RObject>(), s.Intern("@state"u8), value);
                }

                return value;
            });

            module.DefineMethod(mrb.Intern("cmd"u8), (s, self) =>
            {
                var methodTableValue = s.GetConst(s.Intern("PUBLISH_METHOD_TABLE"u8), s.ObjectClass);
                var methodTable = (methodTableValue.As<RData>().Data as Dictionary<Symbol, MRubyPublishDelegate>)!;

                var commandNameSymbol = s.GetArgumentAsSymbolAt(0);
                var props = s.GetKeywordArguments();
                var propsHash = s.NewHash(props.Length);

                foreach (var prop in props)
                {
                    propsHash.Add(prop.Key, prop.Value);
                }

                var fiber = s.CurrentFiber;
                fiber.Yield();
                if (MRubyRoutingScript.TryFindScript(fiber, out var script))
                {
                    _ = ExecuteCommandAsync(script.Router);
                }
                else
                {
                    s.Raise(s.Intern("RuntimeError"u8), $"No such script : {fiber}");
                }

                return MRubyValue.Nil;

                async ValueTask ExecuteCommandAsync(ICommandPublisher publisher)
                {
                    try
                    {
                        if (methodTable.TryGetValue(commandNameSymbol, out var method))
                        {
                            // Ensure the `cmd` method has completed before resuming.
#if VITALROUTER_UNITASK_INTEGRATION
                            await Cysharp.Threading.Tasks.UniTask.Yield(cancellationToken: script.CancellationToken);
                            await method(publisher, mrb, propsHash, script.CancellationToken);
#elif UNITY_2023_1_OR_NEWER
                            await UnityEngine.Awaitable.NextFrameAsync(script.CancellationToken);
                            await method(publisher, mrb, propsHash, script.CancellationToken);
#else
                            await Task.Run(async () => await method(publisher, mrb, propsHash, script.CancellationToken), script.CancellationToken);
#endif
                            // await method(publisher, mrb, propsHash, script.CancellationToken);
                            script.Resume();
                        }
                    }
                    catch (Exception ex)
                    {
                        script.SetException(ex);
                    }
                }
            });
        });

        mrb.IncludeModule(mrb.ObjectClass, module);
    }
}
}
