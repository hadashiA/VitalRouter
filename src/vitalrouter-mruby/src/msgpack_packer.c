#include "mruby.h"
#include "mruby/array.h"
#include "mruby/hash.h"
#include "mruby/string.h"
#include "msgpack_packer.h"
#include <string.h>

void
msgpack_packer_static_destroy()
{ }

void
msgpack_packer_init(mrb_state* mrb, msgpack_packer_t* pk)
{
  memset(pk, 0, sizeof(msgpack_packer_t));

  msgpack_buffer_init(PACKER_BUFFER_(pk));

  pk->io = mrb_nil_value();
}

void
msgpack_packer_destroy(mrb_state* mrb, msgpack_packer_t* pk)
{
  msgpack_buffer_destroy(mrb, PACKER_BUFFER_(pk));
}

void
msgpack_packer_mark(mrb_state* mrb, msgpack_packer_t* pk)
{
  //rb_gc_mark(pk->io);
  mrb_gc_mark_value(mrb, pk->io);

  /* See MessagePack_Buffer_wrap */
  /* msgpack_buffer_mark(PACKER_BUFFER_(pk)); */
  //rb_gc_mark(pk->buffer_ref);
  mrb_gc_mark_value(mrb, pk->buffer_ref);
}

void
msgpack_packer_reset(mrb_state* mrb, msgpack_packer_t* pk)
{
  msgpack_buffer_clear(mrb, PACKER_BUFFER_(pk));

  pk->io = mrb_nil_value();
  pk->io_write_all_method = 0;
  pk->buffer_ref = mrb_nil_value();
}

void
msgpack_packer_write_array_value(mrb_state* mrb, msgpack_packer_t* pk, mrb_value v)
{
  /* actual return type of RARRAY_LEN is long */
  unsigned long len = RARRAY_LEN(v);
  if(len > 0xffffffffUL) {
    mrb_raisef(mrb, E_ARGUMENT_ERROR, "size of array is too long to pack: %S bytes should be <= %S",
              mrb_fixnum_value(len), mrb_fixnum_value(0xffffffffUL));
  }
  unsigned int len32 = (unsigned int)len;
  msgpack_packer_write_array_header(mrb, pk, len32);

  unsigned int i;
  for (i = 0; i < len32; i++) {
    mrb_value e = mrb_ary_entry(v, i);
    msgpack_packer_write_value(mrb, pk, e);
  }
}

static void
write_hash_foreach(mrb_state* mrb, mrb_value key, mrb_value value, msgpack_packer_t* pk_value)
{
  //if (key == Qundef) {
  if (mrb_type(key) == MRB_TT_UNDEF) {
    //return ST_CONTINUE; /* RUBY C */
    return;
  }
  msgpack_packer_t* pk = (msgpack_packer_t*) pk_value;
  msgpack_packer_write_value(mrb, pk, key);
  msgpack_packer_write_value(mrb, pk, value);
}

void
msgpack_packer_write_hash_value(mrb_state* mrb, msgpack_packer_t* pk, mrb_value hash)
{
  /* actual return type of RHASH_SIZE is long (if SIZEOF_LONG == SIZEOF_VOIDP
   * or long long (if SIZEOF_LONG_LONG == SIZEOF_VOIDP. See st.h. */
  mrb_value hash_keys_ary = mrb_hash_keys(mrb, hash);
  unsigned long len = RARRAY_LEN(hash_keys_ary);
  unsigned int len32 = 0;
  mrb_value key;

  if (len > 0xfffffff) {
    mrb_raisef(mrb, E_ARGUMENT_ERROR, "size of array is too long to pack: %S bytes should be <= %S",
               mrb_fixnum_value(len), mrb_fixnum_value(0xffffffffUL));
  }

  len32 = (unsigned int)len;
  msgpack_packer_write_map_header(mrb, pk, len32);
  while (TRUE) {
    key = mrb_ary_pop(mrb, hash_keys_ary);
    if (mrb_nil_p(key)) return;
    write_hash_foreach(mrb, key, mrb_hash_get(mrb, hash, key), pk);
  }
}

static void
_msgpack_packer_write_other_value(mrb_state* mrb, msgpack_packer_t* pk, mrb_value v)
{
  mrb_funcall_argv(mrb, v, pk->to_msgpack_method, 1, &pk->to_msgpack_arg);
}

void
msgpack_packer_write_value(mrb_state* mrb, msgpack_packer_t* pk, mrb_value v)
{
  if (mrb_nil_p(v)) {
    msgpack_packer_write_nil(mrb, pk);
    return;
  }
  switch (mrb_type(v)) {
  case MRB_TT_TRUE:
    msgpack_packer_write_true(mrb, pk);
    return;
  case MRB_TT_FALSE:
    msgpack_packer_write_false(mrb, pk);
    return;
  case MRB_TT_FIXNUM:
    msgpack_packer_write_fixnum_value(mrb, pk, v);
    return;
  case MRB_TT_SYMBOL:
    msgpack_packer_write_symbol_value(mrb, pk, v);
    return;
  case MRB_TT_STRING:
    msgpack_packer_write_string_value(mrb, pk, v);
    return;
  case MRB_TT_ARRAY:
    msgpack_packer_write_array_value(mrb, pk, v);
    return;
  case MRB_TT_HASH:
    msgpack_packer_write_hash_value(mrb, pk, v);
    return;
  case MRB_TT_FLOAT:
    msgpack_packer_write_float_value(mrb, pk, v);
    return;
  default:
    _msgpack_packer_write_other_value(mrb, pk, v);
  }
}
