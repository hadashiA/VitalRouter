#include <mruby.h>
#include <mruby/presym.h>
#include <mruby/error.h>
#include <mruby/string.h>
#include <mruby/data.h>
#include <mruby/class.h>
#include <mruby/object.h>
#include <mruby/variable.h>
#include "vitalrouter-mruby.h"

#define SCRIPT_RUNNING_LIMIT 16
#define fiber_ptr(o) ((struct RFiber*)mrb_ptr(o))

typedef struct {
  mrb_int fiber_obj_id;
  vitalrouter_mrb_script *script;
} running_script_entry;

static int last_script_id = 0;
static running_script_entry running_script_entries[SCRIPT_RUNNING_LIMIT];

mrb_value mrb_get_backtrace(mrb_state*);

static int32_t throw(vitalrouter_mrb_ctx *ctx, int32_t script_id) {
  if (ctx->mrb->exc) {
    if (ctx->on_error) {
      mrb_value exc_backtrace = mrb_get_backtrace(ctx->mrb);
      mrb_value exc_inspection = mrb_funcall(ctx->mrb, mrb_obj_value(ctx->mrb->exc), "inspect", 0, NULL);
      if (mrb_test(exc_backtrace)) {
        mrb_value exc_backtrace_lines = mrb_funcall(ctx->mrb, exc_backtrace, "join", 1, mrb_str_new_cstr(ctx->mrb, "\n"));
        mrb_funcall(ctx->mrb, exc_inspection, "<<", 1, exc_backtrace_lines);
      }
      ctx->on_error(script_id, mrb_str_to_cstr(ctx->mrb, exc_inspection));
    }
    return 1;
  }
  return 0;
}

static int32_t running_script_entries_add(vitalrouter_mrb_script *script) {
  mrb_int id = mrb_obj_id(script->fiber);
  for (int i = 0; i < SCRIPT_RUNNING_LIMIT; i++) {
    if (running_script_entries[i].script == NULL ||
        running_script_entries[i].fiber_obj_id == id) {
      running_script_entries[i].fiber_obj_id = id;
      running_script_entries[i].script = script;
      return VITALROUTER_OK;
    }
  }
  return VITALROUTER_ERROR;
}

static vitalrouter_mrb_script *running_script_entries_get(mrb_int id) {
  for (int i = 0; i < SCRIPT_RUNNING_LIMIT; i++) {
    running_script_entry x = running_script_entries[i];
    if (x.script != NULL && x.fiber_obj_id == id) {
      return x.script;
    }
  }
  return NULL;
}

static void running_script_entries_remove(mrb_value fiber) {
  mrb_int id = mrb_obj_id(fiber);
  for (int i = 0; i < SCRIPT_RUNNING_LIMIT; i++) {
    running_script_entry x = running_script_entries[i];
    if (x.fiber_obj_id == id) {
      running_script_entries[i].fiber_obj_id = 0;
      running_script_entries[i].script = NULL;
    }
  }
}

static void shared_state_set(mrb_state *mrb, char *key, mrb_value value) {
  mrb_value self = mrb_obj_value(mrb->top_self);
  mrb_value state = mrb_funcall(mrb, self, "state", 0);
  mrb_value sym = mrb_symbol_value(mrb_intern_cstr(mrb, key));
  mrb_funcall(mrb, state, "[]=", 2, sym, value);
}

extern vitalrouter_mrb_ctx *vitalrouter_mrb_ctx_new()
{
  mrb_state *mrb = mrb_open();
  if (mrb == NULL) {
    return NULL;
  }

  vitalrouter_mrb_ctx *ctx = mrb_malloc(mrb, sizeof(vitalrouter_mrb_ctx));
  ctx->mrb = mrb;
  return ctx;
}

extern void vitalrouter_mrb_ctx_dispose(vitalrouter_mrb_ctx *ctx)
{
  mrb_close(ctx->mrb);
}

extern void vitalrouter_mrb_callbacks_set(vitalrouter_mrb_ctx *ctx,
                                          vitalrouter_mrb_command_cb on_command,
                                          vitalrouter_mrb_error_cb on_error)
{
  ctx->on_command = on_command;
  ctx->on_error = on_error;
}

extern void vitalrouter_mrb_state_set_int32(vitalrouter_mrb_ctx *ctx, char *key, int32_t value)
{
  shared_state_set(ctx->mrb, key, mrb_fixnum_value(value));
}

extern void vitalrouter_mrb_state_set_float(vitalrouter_mrb_ctx *ctx, char *key, float_t value)
{
  shared_state_set(ctx->mrb, key, mrb_float_value(ctx->mrb, value));
}

extern void vitalrouter_mrb_state_set_bool(vitalrouter_mrb_ctx *ctx, char *key, int32_t value)
{
  shared_state_set(ctx->mrb, key, value == 0 ? mrb_false_value() : mrb_true_value());
}

extern void vitalrouter_mrb_state_set_string(vitalrouter_mrb_ctx *ctx, char *key, char *value)
{
  shared_state_set(ctx->mrb, key, mrb_str_new_cstr(ctx->mrb, value));
}

extern void vitalrouter_mrb_state_remove(vitalrouter_mrb_ctx *ctx,char *key) {
  mrb_value self = mrb_obj_value(ctx->mrb->top_self);
  mrb_value state = mrb_funcall(ctx->mrb, self, "state", 0);
  mrb_value sym = mrb_symbol_value(mrb_intern_cstr(ctx->mrb, key));
  mrb_funcall(ctx->mrb, state, "delete", 1, sym);
}

extern void vitalrouter_mrb_state_clear(vitalrouter_mrb_ctx *ctx) {
  mrb_value self = mrb_obj_value(ctx->mrb->top_self);
  mrb_value state = mrb_funcall(ctx->mrb, self, "state", 0);
  mrb_funcall(ctx->mrb, state, "clear", 0, NULL);
}


extern void vitalrouter_mrb_load(vitalrouter_mrb_ctx *ctx, vitalrouter_mrb_source source)
{
  int ai = mrb_gc_arena_save(ctx->mrb);

  mrb_load_nstring(ctx->mrb, (const char *)source.bytes, source.length);
  throw(ctx, -1);
  mrb_gc_arena_restore(ctx->mrb, ai);
}


extern vitalrouter_mrb_script *vitalrouter_mrb_script_compile(vitalrouter_mrb_ctx *ctx,
                                                              vitalrouter_mrb_source source)
{
  int ai = mrb_gc_arena_save(ctx->mrb);

  mrb_ccontext *compiler_ctx = mrb_ccontext_new(ctx->mrb);
  compiler_ctx->no_exec = TRUE;

  mrb_value proc = mrb_load_nstring_cxt(ctx->mrb, (const char *)source.bytes, (size_t)source.length, compiler_ctx);
  if (throw(ctx, -1)) {
    return NULL;
  }
  
  if (!mrb_proc_p(proc)) {
    mrb_ccontext_free(ctx->mrb, compiler_ctx);
    mrb_gc_arena_restore(ctx->mrb, ai);
    return NULL;
  }

  vitalrouter_mrb_script *script = mrb_malloc(ctx->mrb, sizeof(vitalrouter_mrb_script));
  script->id = ++last_script_id;
  script->compiler_context = compiler_ctx;
  script->proc = proc;
  script->fiber = mrb_nil_value();
  script->on_command = ctx->on_command;

  mrb_gc_register(ctx->mrb, script->proc);
  mrb_gc_arena_restore(ctx->mrb, ai);
  
  return script;
}


extern void vitalrouter_mrb_script_dispose(vitalrouter_mrb_ctx *ctx,
                                           vitalrouter_mrb_script *script)
{
  running_script_entries_remove(script->fiber);
  mrb_ccontext_free(ctx->mrb, script->compiler_context);
  mrb_gc_unregister(ctx->mrb, script->proc);
  mrb_gc_unregister(ctx->mrb, script->fiber);
  mrb_free(ctx->mrb, script);
}

extern int32_t vitalrouter_mrb_script_status(vitalrouter_mrb_ctx *ctx,
                                             vitalrouter_mrb_script *script)
{
  if (mrb_fiber_p(script->fiber)) {
    return fiber_ptr(script->fiber)->cxt->status;
  }
  return -1;
}

extern int32_t vitalrouter_mrb_script_start(vitalrouter_mrb_ctx *ctx,
                                            vitalrouter_mrb_script *script)
{
  int ai = mrb_gc_arena_save(ctx->mrb);
  
  if (mrb_test(script->fiber)) {
    running_script_entries_remove(script->fiber);
    mrb_gc_unregister(ctx->mrb, script->fiber);
    enum mrb_fiber_state status = fiber_ptr(script->fiber)->cxt->status;
    if (status != MRB_FIBER_TERMINATED) {
      return VITALROUTER_ERROR;
    }
  }

  script->fiber = mrb_fiber_new(ctx->mrb, mrb_proc_ptr(script->proc));
  int32_t added = running_script_entries_add(script);
  if (added == VITALROUTER_ERROR) {
    mrb_gc_arena_restore(ctx->mrb, ai);
    return VITALROUTER_ERROR;
  }

  mrb_gc_register(ctx->mrb, script->fiber);
  mrb_gc_arena_restore(ctx->mrb, ai);

  return vitalrouter_mrb_script_resume(ctx, script);
}


extern int32_t vitalrouter_mrb_script_resume(vitalrouter_mrb_ctx *ctx,
                                             vitalrouter_mrb_script *script)
{
  if (!mrb_test(script->fiber)) {
    return VITALROUTER_ERROR;
  }

  int ai = mrb_gc_arena_save(ctx->mrb);

  mrb_fiber_resume(ctx->mrb, script->fiber, 0, NULL);
  if (throw(ctx, script->id)) {
    return VITALROUTER_ERROR;
  }

  mrb_value aliving = mrb_fiber_alive_p(ctx->mrb, script->fiber);
  if (mrb_test(aliving)) {
    mrb_gc_arena_restore(ctx->mrb, ai);
    return VITALROUTER_CONTINUE;
  } else {
    running_script_entries_remove(script->fiber);
    mrb_gc_unregister(ctx->mrb, script->fiber);
    // script->fiber = mrb_nil_value();
    
    mrb_gc_arena_restore(ctx->mrb, ai);
    
    return VITALROUTER_DONE;
  }
}

mrb_value mrb_vitalrouter_cmd(mrb_state *mrb, mrb_value self) {
  char *name, *payload;
  mrb_int name_len, payload_len;
  mrb_value fiber;
  mrb_get_args(mrb, "oss", &fiber, &name, &name_len, &payload, &payload_len);

  vitalrouter_mrb_script *script = running_script_entries_get(mrb_obj_id(fiber));
  if (script == NULL) {
    mrb_raise(mrb, E_RUNTIME_ERROR, "invalid condition. VitalRouter script meta data does not exists.");
  }
  
  mrb_fiber_yield(mrb, 0, NULL);
  script->on_command(script->id, (uint8_t *)name, name_len, (uint8_t *)payload, payload_len);
  return mrb_nil_value();
}

void mrb_vitalrouter_mruby_gem_init(mrb_state *mrb) {
  struct RClass *module = mrb_define_module(mrb, "VitalRouter");
  mrb_define_method(mrb, module, "__cmd", mrb_vitalrouter_cmd, MRB_ARGS_REQ(2));
}

void mrb_vitalrouter_mruby_gem_final(mrb_state *mrb)
{
}
