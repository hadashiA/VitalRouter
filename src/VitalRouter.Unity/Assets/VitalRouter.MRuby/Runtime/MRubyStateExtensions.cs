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
    RHash props);

public static class MRubyStateExtensions
{
    public static void AddVitalRouter(this MRubyState state, Action<MRubyState> configure)
    {
        if (!state.TryGetConst(state.Intern("VitalRouter"u8), out _))
        {
            state.DefineVitalRouter();
        }
        configure(state);
    }

    public static void AddCommand<TCommand>(this MRubyState state, string name) where TCommand : ICommand
    {
        var commandTable = state.GetCommandTable();
        var key = state.Intern(name);
        commandTable[key] = async (publisher, s, props) =>
        {
            var cmd = MRubyValueSerializer.Deserialize<TCommand>(props, s)!;
            await publisher.PublishAsync(cmd);
        };
    }

    public static MRubySharedVariableTable GetSharedVariables(this MRubyState state)
    {
        if (!state.TryGetConst(state.Intern("VitalRouter"u8), out _))
        {
            DefineVitalRouter(state);
        }
        var instance = state.Send(state.ObjectClass, state.Intern("state"u8));
        var tableVariable = state.GetInstanceVariable(instance.As<RObject>(), state.Intern("@table"u8));
        var data = tableVariable.As<RData>().Data;
        return (MRubySharedVariableTable)data;
    }

    static Dictionary<Symbol, MRubyPublishDelegate> GetCommandTable(this MRubyState state)
    {
        if (!state.TryGetConst(state.Intern("VitalRouter"u8), out _))
        {
            state.DefineVitalRouter();
        }

        var methodTableValue = state.GetConst(state.Intern("PUBLISH_METHOD_TABLE"u8), state.ObjectClass);
        return (methodTableValue.As<RData>().Data as Dictionary<Symbol, MRubyPublishDelegate>)!;
    }

    public static async ValueTask ExecuteAsync(
        this MRubyState state,
        Router router,
        Irep irep,
        CancellationToken cancellation = default)
    {
        var proc = state.CreateProc(irep);
        var fiber = state.CreateFiber(proc);

        var publisher = router.WithFilter(new MRubyStateInterceptor(state));

        using var script = new MRubyRoutingScript(fiber, publisher);
        await script.RunAsync(cancellation);
    }

    public static void DefineVitalRouter(this MRubyState state)
    {
        var module = state.DefineModule(state.Intern("VitalRouter"u8), state.ObjectClass, module =>
        {
            var methodTable = new Dictionary<Symbol, MRubyPublishDelegate>();
            module.DefineConst(state.Intern("PUBLISH_METHOD_TABLE"u8), new RData(methodTable));

            var sharedStateClass = module.DefineClass(state.Intern("SharedState"u8), sharedStateClass =>
            {
                sharedStateClass.DefineMethod(state.Intern("initialize"u8), (s, self) =>
                {
                    var table = new MRubySharedVariableTable(state);
                    var data = new RData(table);
                    s.SetInstanceVariable(self.As<RObject>(), s.Intern("@table"u8), data);
                    return self;
                });

                sharedStateClass.DefineMethod(state.Intern("[]"u8), (s, self) =>
                {
                    var key = s.Intern(s.Stringify(s.GetArgumentAt(0)));
                    var instance = state.Send(state.ObjectClass, state.Intern("state"u8));
                    var tableVariable = state.GetInstanceVariable(instance.As<RObject>(), state.Intern("@table"u8));
                    var table = (MRubySharedVariableTable)tableVariable.As<RData>().Data;
                    return table.GetOrNil(key);
                });

                sharedStateClass.DefineMethod(state.Intern("[]="u8), (s, self) =>
                {
                    var key = s.Intern(s.Stringify(s.GetArgumentAt(0)));
                    var value = s.GetArgumentAt(1);

                    var instance = state.Send(state.ObjectClass, state.Intern("state"u8));
                    var tableVariable = state.GetInstanceVariable(instance.As<RObject>(), state.Intern("@table"u8));
                    var table = (MRubySharedVariableTable)tableVariable.As<RData>().Data;
                    table.Set(key, value);
                    return value;
                });
            });

            module.DefineMethod(state.Intern("log"u8), (s, self) =>
            {
                var message = s.Stringify(s.GetArgumentAt(0));
                UnityEngine.Debug.Log(message);
                return MRubyValue.Nil;
            });

            module.DefineMethod(state.Intern("state"u8), (s, self) =>
            {
                var value = s.GetInstanceVariable(self.As<RObject>(), s.Intern("@state"u8));
                if (value.IsNil)
                {
                    value = s.Send(sharedStateClass, s.Intern("new"u8));
                    s.SetInstanceVariable(self.As<RObject>(), s.Intern("@state"u8), value);
                }

                return value;
            });

            module.DefineMethod(state.Intern("cmd"u8), (s, self) =>
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
                    s.Raise(s.Intern("RuntimeError"u8), $"No such script : {s.CurrentFiber}");
                }

                return MRubyValue.Nil;

                async Task ExecuteCommandAsync(ICommandPublisher publisher)
                {
                    try
                    {
                        if (methodTable.TryGetValue(commandNameSymbol, out var method))
                        {
                            await method(publisher, state, propsHash);
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

        state.IncludeModule(state.ObjectClass, module);
    }
}
}
