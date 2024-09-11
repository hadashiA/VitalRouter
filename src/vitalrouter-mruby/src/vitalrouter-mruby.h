#ifndef VITALROUTER_MRUBY_H
#define VITALROUTER_MRUBY_H

#include <mruby/proc.h>

void mrb_vitalrouter_mruby_init(mrb_state *mrb);
void mrb_vitalrouter_mruby_final(mrb_state *mrb);

#define VITALROUTER_OK        0
#define VITALROUTER_ERROR    -1
#define VITALROUTER_CONTINUE  1
#define VITALROUTER_DONE      2

typedef void (*vitalrouter_mrb_command_cb)(int32_t script_id,
                                           uint8_t *command_name,
                                           int32_t command_name_length,
                                           uint8_t *payload,
                                           int32_t payload_length);
typedef void (*vitalrouter_mrb_error_cb)(int32_t script_id,
                                         char *exception);
typedef struct {
  int32_t id;
  mrb_ccontext *compiler_context;
  mrb_value proc;
  mrb_value fiber;
  vitalrouter_mrb_command_cb on_command;  
} vitalrouter_mrb_script;

typedef struct {
  mrb_state *mrb;
  vitalrouter_mrb_command_cb on_command;
  vitalrouter_mrb_error_cb on_error;
} vitalrouter_mrb_ctx;

typedef struct {
  uint8_t *bytes;
  int32_t length;
} vitalrouter_mrb_source;

extern vitalrouter_mrb_ctx *vitalrouter_mrb_ctx_new();
extern void vitalrouter_mrb_ctx_dispose(vitalrouter_mrb_ctx *ctx);

extern void vitalrouter_mrb_callbacks_set(vitalrouter_mrb_ctx *ctx,
                                          vitalrouter_mrb_command_cb on_command,
                                          vitalrouter_mrb_error_cb on_error);
extern void vitalrouter_mrb_state_set_int32(vitalrouter_mrb_ctx *ctx, char *key,int32_t value);
extern void vitalrouter_mrb_state_set_float(vitalrouter_mrb_ctx *ctx, char *key, float value);
extern void vitalrouter_mrb_state_set_bool(vitalrouter_mrb_ctx *ctx, char *key,int32_t value);
extern void vitalrouter_mrb_state_set_string(vitalrouter_mrb_ctx *ctx, char *key, char *value);
extern void vitalrouter_mrb_state_remove(vitalrouter_mrb_ctx *ctx, char *key);
extern void vitalrouter_mrb_state_clear(vitalrouter_mrb_ctx *ctx);

extern void vitalrouter_mrb_load(vitalrouter_mrb_ctx *ctx,
                                 vitalrouter_mrb_source source);

extern vitalrouter_mrb_script *vitalrouter_mrb_script_compile(vitalrouter_mrb_ctx *ctx,
                                                              vitalrouter_mrb_source source);
extern void vitalrouter_mrb_script_dispose(vitalrouter_mrb_ctx *ctx,
                                           vitalrouter_mrb_script *script);
extern int32_t vitalrouter_mrb_script_status(vitalrouter_mrb_ctx *ctx,
                                             vitalrouter_mrb_script *script);
extern int32_t vitalrouter_mrb_script_start(vitalrouter_mrb_ctx *ctx,
                                            vitalrouter_mrb_script *script);
extern int32_t vitalrouter_mrb_script_resume(vitalrouter_mrb_ctx *ctx,
                                             vitalrouter_mrb_script *script);
#endif
