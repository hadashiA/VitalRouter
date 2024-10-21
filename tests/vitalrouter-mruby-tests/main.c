#include <stdlib.h>
#include <stdio.h>
#include <mruby.h>
#include <vitalrouter-mruby.h>

#define RUBY_SOURCE \
  "cmd :a, pos: { x: 123, y: 456 }\n"           \
  "cmd :b, pos: { x: 789, y: 321 }\n"           \
  "cmd :text, body: 'Hello, Hello, Heeello'\n"           \
  "\n"

vitalrouter_mrb_ctx *ctx;
vitalrouter_mrb_script *script;

static char *cstr(char *nstr, int32_t len) {
  char *result = (char*)malloc(len + 1);
  if (result == NULL) {
    return NULL;
  }

  strncpy(result, nstr, len);
  result[len] = '\0';
  return result;
}

void on_command(int32_t script_id, vitalrouter_nstring command_name, mrb_value payload) {

  char *command_name_cstr = cstr((char *)command_name.bytes, command_name.length);
  printf("[CALLBACK(%d)] cmd:%s\n", script_id, command_name_cstr);
  uint32_t result = vitalrouter_mrb_script_resume(ctx, script);
  printf("RESUMED: %d\n", result);
  
  free(command_name_cstr);
}

void on_error(int32_t script_id, char *exception_inspection) {
  printf("[CALLBACK] ERROR %s", exception_inspection);
}

int main() {
  size_t len = strlen(RUBY_SOURCE);
  uint8_t *buf = (uint8_t *)malloc(len * sizeof(uint8_t));
  memcpy(buf, RUBY_SOURCE, len);
  vitalrouter_nstring source = { buf, len };

  ctx = vitalrouter_mrb_ctx_new();
  if (ctx == NULL) {
    printf("Failed to create ctx!!\n");
    return 1;
  }
  printf("CREATED ctx\n");  
  
  vitalrouter_mrb_callbacks_set(ctx, on_command, on_error);
  printf("CALLBACKS set\n");

  /* vitalrouter_nstring key1 = { (uint8_t *)"i", 1 }; */
  /* vitalrouter_nstring key2 = { (uint8_t *)"b", 1 }; */
  /* vitalrouter_mrb_state_set_int32(ctx, key1, 123); */
  /* vitalrouter_mrb_state_set_int32(ctx, key2, 1); */

  script = vitalrouter_mrb_script_compile(ctx, source);
  printf("COMPILED id:%d\n", script->id);
  
  uint32_t result = vitalrouter_mrb_script_start(ctx, script);
  printf("STARTED: %d\n", result);

  vitalrouter_mrb_script_dispose(ctx, script);
  vitalrouter_mrb_ctx_dispose(ctx);

  if (result == VITALROUTER_DONE) {
    printf("DONE\n");
    return 0;
  } else {
    printf("INVALID RESULT %d\n", result);
    return 1;
  }
}
