#ifndef MSGPACK_MRUBY_PACKER_H__
#define MSGPACK_MRUBY_PACKER_H__

#if defined(__cplusplus)
extern "C" {
#endif

#include <mruby.h>
#include <mruby/string.h>
#include <mruby/value.h>
#include "msgpack_buffer.h"

struct msgpack_packer_t;
typedef struct msgpack_packer_t msgpack_packer_t;

struct msgpack_packer_t {
    msgpack_buffer_t buffer;

    mrb_value io;
    mrb_sym io_write_all_method;

    mrb_sym to_msgpack_method;
    mrb_value to_msgpack_arg;

    mrb_value buffer_ref;
};

#define PACKER_BUFFER_(pk) (&(pk)->buffer)

void msgpack_packer_static_init();

void msgpack_packer_static_destroy();

void msgpack_packer_init(mrb_state *mrb, msgpack_packer_t* pk);

void msgpack_packer_destroy(mrb_state *mrb, msgpack_packer_t* pk);

void msgpack_packer_mark(mrb_state *mrb, msgpack_packer_t* pk);

static inline void
msgpack_packer_set_to_msgpack_method(msgpack_packer_t* pk, mrb_sym to_msgpack_method, mrb_value to_msgpack_arg)
{
    pk->to_msgpack_method = to_msgpack_method;
    pk->to_msgpack_arg = to_msgpack_arg;
}

static inline void msgpack_packer_set_io(msgpack_packer_t* pk, mrb_value io, mrb_sym io_write_all_method)
{
    pk->io = io;
    pk->io_write_all_method = io_write_all_method;
}

void msgpack_packer_reset(mrb_state *mrb, msgpack_packer_t* pk);


static inline void
msgpack_packer_write_nil(mrb_state *mrb, msgpack_packer_t* pk)
{
  msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 1);
  msgpack_buffer_write_1(PACKER_BUFFER_(pk), 0xc0);
}

static inline void
msgpack_packer_write_true(mrb_state *mrb, msgpack_packer_t* pk)
{
  msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 1);
  msgpack_buffer_write_1(PACKER_BUFFER_(pk), 0xc3);
}

static inline void
msgpack_packer_write_false(mrb_state *mrb, msgpack_packer_t* pk)
{
  msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 1);
  msgpack_buffer_write_1(PACKER_BUFFER_(pk), 0xc2);
}

static inline void _msgpack_packer_write_fixint(mrb_state *mrb, msgpack_packer_t* pk, int8_t v)
{
    msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 1);
    msgpack_buffer_write_1(PACKER_BUFFER_(pk), v);
}

static inline void
_msgpack_packer_write_uint8(mrb_state *mrb, msgpack_packer_t* pk, uint8_t v)
{
  msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 2);
  msgpack_buffer_write_2(PACKER_BUFFER_(pk), 0xcc, v);
}

static inline void
_msgpack_packer_write_uint16(mrb_state *mrb, msgpack_packer_t* pk, uint16_t v)
{
  msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 3);
  uint16_t be = _msgpack_be16(v);
  msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xcd, (const void*)&be, 2);
}

static inline void
_msgpack_packer_write_uint32(mrb_state *mrb, msgpack_packer_t* pk, uint32_t v)
{
  msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 5);
  uint32_t be = _msgpack_be32(v);
  msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xce, (const void*)&be, 4);
}

static inline void
_msgpack_packer_write_uint64(mrb_state *mrb, msgpack_packer_t* pk, uint64_t v)
{
  msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 9);
  uint64_t be = _msgpack_be64(v);
  msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xcf, (const void*)&be, 8);
}

static inline void
_msgpack_packer_write_int8(mrb_state *mrb, msgpack_packer_t* pk, int8_t v)
{
  msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 2);
  msgpack_buffer_write_2(PACKER_BUFFER_(pk), 0xd0, v);
}

static inline void
_msgpack_packer_write_int16(mrb_state *mrb, msgpack_packer_t* pk, int16_t v)
{
  msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 3);
  uint16_t be = _msgpack_be16(v);
  msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xd1, (const void*)&be, 2);
}

static inline void
_msgpack_packer_write_int32(mrb_state *mrb, msgpack_packer_t* pk, int32_t v)
{
  msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 5);
  uint32_t be = _msgpack_be32(v);
  msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xd2, (const void*)&be, 4);
}

static inline void
_msgpack_packer_write_int64(mrb_state *mrb, msgpack_packer_t* pk, int64_t v)
{
  msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 9);
  uint64_t be = _msgpack_be64(v);
  msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xd3, (const void*)&be, 8);
}

static inline void
_msgpack_packer_write_long32(mrb_state *mrb, msgpack_packer_t* pk, long v)
{
  if (v < -0x20L) {
    if (v < -0x8000L) {
      _msgpack_packer_write_int32(mrb, pk, (int32_t) v);
    } else if(v < -0x80L) {
      _msgpack_packer_write_int16(mrb, pk, (int16_t) v);
    } else {
      _msgpack_packer_write_int8(mrb, pk, (int8_t) v);
    }
  } else if (v <= 0x7fL) {
    _msgpack_packer_write_fixint(mrb, pk, (int8_t) v);
  } else {
    if (v <= 0xffL) {
      _msgpack_packer_write_uint8(mrb, pk, (uint8_t) v);
    } else if (v <= 0xffffL) {
      _msgpack_packer_write_uint16(mrb, pk, (uint16_t) v);
    } else {
      _msgpack_packer_write_uint32(mrb, pk, (uint32_t) v);
    }
  }
}

static inline void
_msgpack_packer_write_long_long64(mrb_state *mrb, msgpack_packer_t* pk, long long v)
{
  if (v < -0x20LL) {
    if(v < -0x8000LL) {
      if (v < -0x80000000LL) {
        _msgpack_packer_write_int64(mrb, pk, (int64_t) v);
      } else {
        _msgpack_packer_write_int32(mrb, pk, (int32_t) v);
      }
    } else {
      if (v < -0x80LL) {
        _msgpack_packer_write_int16(mrb, pk, (int16_t) v);
      } else {
        _msgpack_packer_write_int8(mrb, pk, (int8_t) v);
       }
    }
  } else if (v <= 0x7fLL) {
    _msgpack_packer_write_fixint(mrb, pk, (int8_t) v);
  } else {
    if (v <= 0xffffLL) {
      if (v <= 0xffLL) {
        _msgpack_packer_write_uint8(mrb, pk, (uint8_t) v);
      } else {
        _msgpack_packer_write_uint16(mrb, pk, (uint16_t) v);
      }
    } else {
      if (v <= 0xffffffffLL) {
        _msgpack_packer_write_uint32(mrb, pk, (uint32_t) v);
      } else {
        _msgpack_packer_write_uint64(mrb, pk, (uint64_t) v);
      }
    }
  }
}

static inline void
msgpack_packer_write_long(mrb_state *mrb, msgpack_packer_t* pk, long v)
{
#if defined(SIZEOF_LONG)
#  if SIZEOF_LONG <= 4
    _msgpack_packer_write_long32(mrb, k, v);
#  else
    _msgpack_packer_write_long_long64(mrb, pk, v);
#  endif

#elif defined(LONG_MAX)
#  if LONG_MAX <= 0x7fffffffL
    _msgpack_packer_write_long32(mrb, pk, v);
#  else
    _msgpack_packer_write_long_long64(mrb, pk, v);
#  endif

#else
  if (sizeof(long) <= 4) {
   _msgpack_packer_write_long32(mrb, pk, v);
  } else {
   _msgpack_packer_write_long_long64(mrb, pk, v);
  }
#endif
}

static inline void
msgpack_packer_write_long_long(mrb_state *mrb, msgpack_packer_t* pk, long long v)
{
  /* assuming sizeof(long long) == 8 */
  _msgpack_packer_write_long_long64(mrb, pk, v);
}

static inline void msgpack_packer_write_u64(mrb_state *mrb, msgpack_packer_t* pk, uint64_t v)
{
  if (v <= 0xffULL) {
    if (v <= 0x7fULL) {
      _msgpack_packer_write_fixint(mrb, pk, (int8_t) v);
    } else {
      _msgpack_packer_write_uint8(mrb, pk, (uint8_t) v);
    }
  } else {
    if (v <= 0xffffULL) {
      _msgpack_packer_write_uint16(mrb, pk, (uint16_t) v);
    } else if (v <= 0xffffffffULL) {
      _msgpack_packer_write_uint32(mrb, pk, (uint32_t) v);
    } else {
       _msgpack_packer_write_uint64(mrb, pk, (uint64_t) v);
    }
  }
}

static inline void
msgpack_packer_write_double(mrb_state *mrb, msgpack_packer_t* pk, double v)
{
  msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 9);
  union {
    double d;
    uint64_t u64;
    char mem[8];
  } castbuf = { v };
  castbuf.u64 = _msgpack_be_double(castbuf.u64);
  msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xcb, castbuf.mem, 8);
}

static inline void
msgpack_packer_write_raw_header(mrb_state *mrb, msgpack_packer_t* pk, unsigned int n)
{
  if (n < 32) {
    msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 1);
    unsigned char h = 0xa0 | (uint8_t) n;
    msgpack_buffer_write_1(PACKER_BUFFER_(pk), h);
  } else if (n < 65536) {
    msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 3);
    uint16_t be = _msgpack_be16(n);
    msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xda, (const void*)&be, 2);
  } else {
    msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 5);
    uint32_t be = _msgpack_be32(n);
    msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xdb, (const void*)&be, 4);
  }
}

static inline void
msgpack_packer_write_array_header(mrb_state *mrb, msgpack_packer_t* pk, unsigned int n)
{
  if (n < 16) {
    msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 1);
    unsigned char h = 0x90 | (uint8_t) n;
    msgpack_buffer_write_1(PACKER_BUFFER_(pk), h);
  } else if (n < 65536) {
    msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 3);
    uint16_t be = _msgpack_be16(n);
    msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xdc, (const void*)&be, 2);
  } else {
    msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 5);
    uint32_t be = _msgpack_be32(n);
    msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xdd, (const void*)&be, 4);
  }
}

static inline void msgpack_packer_write_map_header(mrb_state *mrb, msgpack_packer_t* pk, unsigned int n)
{
  if (n < 16) {
    msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 1);
    unsigned char h = 0x80 | (uint8_t) n;
    msgpack_buffer_write_1(PACKER_BUFFER_(pk), h);
  } else if(n < 65536) {
    msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 3);
    uint16_t be = _msgpack_be16(n);
    msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xde, (const void*)&be, 2);
  } else {
    msgpack_buffer_ensure_writable(mrb, PACKER_BUFFER_(pk), 5);
    uint32_t be = _msgpack_be32(n);
    msgpack_buffer_write_byte_and_data(PACKER_BUFFER_(pk), 0xdf, (const void*)&be, 4);
  }
}


void _msgpack_packer_write_string_to_io(msgpack_packer_t* pk, mrb_value string);

static inline void msgpack_packer_write_string_value(mrb_state *mrb, msgpack_packer_t* pk, mrb_value v)
{
  /* TODO encoding conversion? */
  /* actual return type of RSTRING_LEN is long */
  unsigned long len = RSTRING_LEN(v);
  //unsigned long len = 0;

  if (len > 0xffffffffUL) {
    // TODO E_ARGUMENT_ERROR?
    mrb_raisef(mrb, E_ARGUMENT_ERROR, "size of string is too long to pack: %lu bytes should be <= %lu", len, 0xffffffffUL);
  }
  msgpack_packer_write_raw_header(mrb, pk, (unsigned int)len);
  msgpack_buffer_append_string(mrb, PACKER_BUFFER_(pk), v);
}

static inline void msgpack_packer_write_symbol_value(mrb_state *mrb, msgpack_packer_t* pk, mrb_value v)
{
  //const char* name = rb_id2name(SYM2ID(v));
  //const char* name = mrb_sym_to_s(mrb, v);
  /* Actual return type of mrb_sym2name_len is size_t */
  mrb_int len = 0L;
  const char* name = mrb_sym2name_len(mrb, mrb_symbol(v), &len);

  //unsigned long len = strlen(name);
  if (len > 0xffffffffUL) {
    // TODO rb_eArgError?
    mrb_raisef(mrb, E_ARGUMENT_ERROR, "size of symbol is too long to pack: %lu bytes should be <= %lu", len, 0xffffffffUL);
  }
  msgpack_packer_write_raw_header(mrb, pk, (unsigned int)len);
  msgpack_buffer_append(mrb, PACKER_BUFFER_(pk), name, len);
}

static inline void
msgpack_packer_write_fixnum_value(mrb_state *mrb, msgpack_packer_t* pk, mrb_value v)
{
  msgpack_packer_write_long(mrb, pk, (long)mrb_fixnum(v));
}

static inline void
msgpack_packer_write_bignum_value(msgpack_packer_t* pk, mrb_value v)
{
  /* mruby doesn't have bignum type. */
  //if (RBIGNUM_POSITIVE_P(v)) {
  //  msgpack_packer_write_u64(pk, rb_big2ull(v));
  //} else {
  //  msgpack_packer_write_long_long(pk, rb_big2ll(v));
  //}
}

static inline void
msgpack_packer_write_float_value(mrb_state *mrb, msgpack_packer_t* pk, mrb_value v)
{
  //msgpack_packer_write_double(pk, rb_num2dbl(v));
  msgpack_packer_write_double(mrb, pk, (double)mrb_float(v));
}

void msgpack_packer_write_array_value(mrb_state *mrb, msgpack_packer_t* pk, mrb_value v);

void msgpack_packer_write_hash_value(mrb_state *mrb, msgpack_packer_t* pk, mrb_value v);

void msgpack_packer_write_value(mrb_state *mrb, msgpack_packer_t* pk, mrb_value v);

#if defined(__cplusplus)
}  /* extern "C" { */
#endif

#endif /* MSGPACK_MRUBY_PACKER_H__ */
