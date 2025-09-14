using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MRubyCS;
using MRubyCS.Serializer;

namespace VitalRouter.MRuby;

public delegate ValueTask MRubyPublishDelegate(
    ICommandPublisher publisher,
    MRubyState mrb,
    RHash props,
    CancellationToken cancellation);

public delegate void MRubyLoggingDelegate(
    MRubyState mrb,
    MRubyValue message);

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

    public static void AddCommand<TCommand>(this MRubyState mrb, string name) where TCommand : ICommand
    {
        var commandTable = mrb.GetCommandTable();
        var key = mrb.Intern(name);
        commandTable[key] = async (publisher, s, props, cancellation) =>
        {
            var cmd = MRubyValueSerializer.Deserialize<TCommand>(props, s)!;
            await publisher.PublishAsync(cmd, cancellation);
        };
    }

    public static void AddLogger(this MRubyState mrb, MRubyLoggingDelegate loggingAction)
    {
        mrb.SetInstanceVariable(mrb.ObjectClass, mrb.Intern("@vitalrouter_logger"u8), new RData(loggingAction));
    }

    public static MRubySharedVariableTable GetSharedVariables(this MRubyState mrb)
    {
        if (!mrb.TryGetConst(mrb.Intern("VitalRouter"u8), out _))
        {
            DefineVitalRouter(mrb);
        }
        var stateValue = mrb.Send(mrb.ObjectClass, mrb.Intern("state"u8));
        var tableValue = mrb.GetInstanceVariable(stateValue.As<RObject>(), mrb.Intern("@table"));

        return (MRubySharedVariableTable)tableValue.As<RData>().Data;
    }

    static Dictionary<Symbol, MRubyPublishDelegate> GetCommandTable(this MRubyState mrb)
    {
        if (!mrb.TryGetConst(mrb.Intern("VitalRouter"u8), out var module))
        {
            module = mrb.DefineVitalRouter();
        }

        var methodTableValue = mrb.GetConst(mrb.Intern("VITALROUTER_METHOD_TABLE"u8), module.As<RClass>());
        return (methodTableValue.As<RData>().Data as Dictionary<Symbol, MRubyPublishDelegate>)!;
    }

    public static async ValueTask ExecuteAsync(
        this MRubyState mrb,
        Router router,
        Irep irep,
        CancellationToken cancellation = default)
    {
        if (router.HasInterceptor<MRubyStateInterceptor>())
        {
            throw new InvalidOperationException("Cannot execute multiple routers for the same MRubyState.");
        }

        var filter = new MRubyStateInterceptor(mrb);
        router.AddFilter(filter);
        try
        {
            var proc = mrb.CreateProc(irep);
            var fiber = mrb.CreateFiber(proc);

            using var script = new MRubyRoutingScript(fiber, router);
            await script.RunAsync(cancellation);
        }
        finally
        {
            router.RemoveFilter(filter);
        }
    }

    public static RClass DefineVitalRouter(this MRubyState mrb)
    {
        var module = mrb.DefineModule(mrb.Intern("VitalRouter"u8), mrb.ObjectClass, module =>
        {
            var methodTable = new Dictionary<Symbol, MRubyPublishDelegate>();
            module.DefineConst(mrb.Intern("VITALROUTER_METHOD_TABLE"u8), new RData(methodTable));

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
                    var tableValue = s.GetInstanceVariable(self.As<RObject>(), s.Intern("@table"));
                    return ((MRubySharedVariableTable)tableValue.As<RData>().Data).GetOrNil(key);
                });

                sharedStateClass.DefineMethod(mrb.Intern("[]="u8), (s, self) =>
                {
                    var key = s.Intern(s.Stringify(s.GetArgumentAt(0)));
                    var value = s.GetArgumentAt(1);

                    var tableValue = s.GetInstanceVariable(self.As<RObject>(), s.Intern("@table"));
                    ((MRubySharedVariableTable)tableValue.As<RData>().Data).Set(key, value);
                    return value;
                });
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
                if (!mrb.TryGetConst(mrb.Intern("VitalRouter"u8), out var module))
                {
                    module = mrb.DefineVitalRouter();
                }

                var methodTableValue = s.GetConst(s.Intern("VITALROUTER_METHOD_TABLE"u8), module.As<RClass>());
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
                    _ = ExecuteCommandAsync(script, methodTable, s, commandNameSymbol, propsHash);
                }
                else
                {
                    s.Raise(s.Intern("RuntimeError"u8), $"No such script : {s.CurrentFiber}");
                }

                return MRubyValue.Nil;

                async Task ExecuteCommandAsync(
                    MRubyRoutingScript script,
                    Dictionary<Symbol, MRubyPublishDelegate> methodTable,
                    MRubyState mrb,
                    Symbol commandName,
                    RHash commandProps)
                {
                    try
                    {
                        if (methodTable.TryGetValue(commandName, out var method))
                        {
                            await Router.YieldAction(script.CancellationToken);
                            await method(script.Router, mrb, commandProps, script.CancellationToken);
                            script.Resume();
                        }
                        else
                        {
                            script.SetException(new MRubyRoutingException($"No such command name `{mrb.NameOf(commandName)}`"));
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
        return module;
    }
}