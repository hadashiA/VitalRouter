#ifndef MSGPACK_MRUBY_BUFFER_H__
#define MSGPACK_MRUBY_BUFFER_H__

#if defined(__cplusplus)
extern "C" {
#endif

#include <mruby/string.h>
#include <string.h>
#include "msgpack_sysdep.h"


#ifndef MSGPACK_BUFFER_STRING_WRITE_REFERENCE_DEFAULT
#define MSGPACK_BUFFER_STRING_WRITE_REFERENCE_DEFAULT (512*1024)
#endif

/* at least 23 (RSTRING_EMBED_LEN_MAX) bytes */
#ifndef MSGPACK_BUFFER_STRING_WRITE_REFERENCE_MINIMUM
#define MSGPACK_BUFFER_STRING_WRITE_REFERENCE_MINIMUM 256
#endif

#ifndef MSGPACK_BUFFER_STRING_READ_REFERENCE_DEFAULT
#define MSGPACK_BUFFER_STRING_READ_REFERENCE_DEFAULT 256
#endif

/* at least 23 (RSTRING_EMBED_LEN_MAX) bytes */
#ifndef MSGPACK_BUFFER_STRING_READ_REFERENCE_MINIMUM
#define MSGPACK_BUFFER_STRING_READ_REFERENCE_MINIMUM 256
#endif

#ifndef MSGPACK_BUFFER_IO_BUFFER_SIZE_DEFAULT
#define MSGPACK_BUFFER_IO_BUFFER_SIZE_DEFAULT (32*1024)
#endif

#ifndef MSGPACK_BUFFER_IO_BUFFER_SIZE_MINIMUM
#define MSGPACK_BUFFER_IO_BUFFER_SIZE_MINIMUM (1024)
#endif

#define NO_MAPPED_STRING (mrb_fixnum_value(0))

struct msgpack_buffer_chunk_t;
typedef struct msgpack_buffer_chunk_t msgpack_buffer_chunk_t;

struct msgpack_buffer_t;
typedef struct msgpack_buffer_t msgpack_buffer_t;

/*
 * msgpack_buffer_chunk_t
 * +----------------+
 * | filled  | free |
 * +---------+------+
 * ^ first   ^ last
 */
struct msgpack_buffer_chunk_t {
  char* first;
  char* last;
  void* mem;
  msgpack_buffer_chunk_t* next;
  mrb_value mapped_string;  /* RBString or NO_MAPPED_STRING */
};

union msgpack_buffer_cast_block_t {
    char buffer[8];
    uint8_t u8;
    uint16_t u16;
    uint32_t u32;
    uint64_t u64;
    int8_t i8;
    int16_t i16;
    int32_t i32;
    int64_t i64;
    float f;
    double d;
};

struct msgpack_buffer_t {
  char* read_buffer;
  char* tail_buffer_end;

  msgpack_buffer_chunk_t tail;
  msgpack_buffer_chunk_t* head;
  msgpack_buffer_chunk_t* free_list;

#ifndef DISABLE_RMEM
  char* rmem_last;
  char* rmem_end;
  void** rmem_owner;
#endif

  union msgpack_buffer_cast_block_t cast_block;

  mrb_value io;
  mrb_value io_buffer;
  mrb_sym io_write_all_method;
  mrb_sym io_partial_read_method;

  size_t write_reference_threshold;
  size_t read_reference_threshold;
  size_t io_buffer_size;

  mrb_value owner;
};

/*
 * initialization functions
 */
void msgpack_buffer_static_init();

void msgpack_buffer_static_destroy();

void msgpack_buffer_init(msgpack_buffer_t* b);

void msgpack_buffer_destroy(mrb_state* mrb, msgpack_buffer_t* b);

void msgpack_buffer_mark(mrb_state* mrb, msgpack_buffer_t* b);

void msgpack_buffer_clear(mrb_state *mrb, msgpack_buffer_t* b);

static inline void
msgpack_buffer_set_write_reference_threshold(msgpack_buffer_t* b, size_t length)
{
  if (length < MSGPACK_BUFFER_STRING_WRITE_REFERENCE_MINIMUM) {
    length = MSGPACK_BUFFER_STRING_WRITE_REFERENCE_MINIMUM;
  }
  b->write_reference_threshold = length;
}

static inline void
msgpack_buffer_set_read_reference_threshold(msgpack_buffer_t* b, size_t length)
{
  if (length < MSGPACK_BUFFER_STRING_READ_REFERENCE_MINIMUM) {
    length = MSGPACK_BUFFER_STRING_READ_REFERENCE_MINIMUM;
  }
  b->read_reference_threshold = length;
}

static inline void
msgpack_buffer_set_io_buffer_size(msgpack_buffer_t* b, size_t length)
{
  if (length < MSGPACK_BUFFER_IO_BUFFER_SIZE_MINIMUM) {
    length = MSGPACK_BUFFER_IO_BUFFER_SIZE_MINIMUM;
  }
  b->io_buffer_size = length;
}

static inline void
msgpack_buffer_reset_io(msgpack_buffer_t* b)
{
  b->io = mrb_nil_value();
}

static inline mrb_bool
msgpack_buffer_has_io(msgpack_buffer_t* b)
{
  return !mrb_nil_p(b->io);
}

static inline void
msgpack_buffer_reset(mrb_state *mrb, msgpack_buffer_t* b)
{
  msgpack_buffer_clear(mrb, b);
  msgpack_buffer_reset_io(b);
}


/*
 * writer functions
 */

static inline size_t
msgpack_buffer_writable_size(const msgpack_buffer_t* b)
{
  return b->tail_buffer_end - b->tail.last;
}

static inline void
msgpack_buffer_write_1(msgpack_buffer_t* b, int byte)
{
  (*b->tail.last++) = (char) byte;
}

static inline void
msgpack_buffer_write_2(msgpack_buffer_t* b, int byte1, unsigned char byte2)
{
  *(b->tail.last++) = (char) byte1;
  *(b->tail.last++) = (char) byte2;
}

static inline void
msgpack_buffer_write_byte_and_data(msgpack_buffer_t* b, int byte, const void* data, size_t length)
{
  (*b->tail.last++) = (char) byte;

  memcpy(b->tail.last, data, length);
  b->tail.last += length;
}

void _msgpack_buffer_expand(mrb_state *mrb, msgpack_buffer_t* b, const char* data, size_t length, mrb_bool use_flush);

size_t msgpack_buffer_flush_to_io(mrb_state *mrb, msgpack_buffer_t* b, mrb_value io, mrb_sym write_method, mrb_bool consume);

static inline size_t
msgpack_buffer_flush(mrb_state *mrb, msgpack_buffer_t* b)
{
  if (mrb_nil_p(b->io)) {
    return 0;
  }
  return msgpack_buffer_flush_to_io(mrb, b, b->io, b->io_write_all_method, TRUE);
}

static inline void
msgpack_buffer_ensure_writable(mrb_state *mrb, msgpack_buffer_t* b, size_t require)
{
  if (msgpack_buffer_writable_size(b) < require) {
    _msgpack_buffer_expand(mrb, b, NULL, require, TRUE);
   }
}

static inline void
_msgpack_buffer_append_impl(mrb_state *mrb, msgpack_buffer_t* b, const char* data, size_t length, mrb_bool flush_to_io)
{
  int size;

  if (length == 0) {
    return;
  }

  size = msgpack_buffer_writable_size(b);

  if (length <= msgpack_buffer_writable_size(b)) {
    memcpy(b->tail.last, data, length);
    b->tail.last += length;
    return;
  }

  _msgpack_buffer_expand(mrb, b, data, length, flush_to_io);
}

static inline void
msgpack_buffer_append(mrb_state *mrb ,msgpack_buffer_t* b, const char* data, size_t length)
{
  _msgpack_buffer_append_impl(mrb, b, data, length, TRUE);
}

static inline
void msgpack_buffer_append_nonblock(mrb_state *mrb ,msgpack_buffer_t* b, const char* data, size_t length)
{
  _msgpack_buffer_append_impl(mrb, b, data, length, FALSE);
}

void _msgpack_buffer_append_long_string(mrb_state *mrb, msgpack_buffer_t* b, mrb_value string);

static inline size_t msgpack_buffer_append_string(mrb_state *mrb, msgpack_buffer_t* b, mrb_value string)
{
  size_t length = RSTRING_LEN(string);

  if (length > b->write_reference_threshold) {
    _msgpack_buffer_append_long_string(mrb, b, string);
  } else {
    msgpack_buffer_append(mrb, b, RSTRING_PTR(string), length);
  }

  return length;
}


/*
 * IO functions
 */
size_t _msgpack_buffer_feed_from_io(mrb_state *mrb, msgpack_buffer_t* b);

size_t _msgpack_buffer_read_from_io_to_string(mrb_state *mrb, msgpack_buffer_t* b, mrb_value string, size_t length);

size_t _msgpack_buffer_skip_from_io(mrb_state *mrb, msgpack_buffer_t* b, size_t length);


/*
 * reader functions
 */

static inline size_t
msgpack_buffer_top_readable_size(const msgpack_buffer_t* b)
{
  return b->head->last - b->read_buffer;
}

size_t msgpack_buffer_all_readable_size(const msgpack_buffer_t* b);

mrb_bool _msgpack_buffer_shift_chunk(mrb_state *mrb, msgpack_buffer_t* b);

static inline void
_msgpack_buffer_consumed(mrb_state *mrb, msgpack_buffer_t* b, size_t length)
{
  b->read_buffer += length;
  if (b->read_buffer >= b->head->last) {
    _msgpack_buffer_shift_chunk(mrb, b);
  }
}

static inline int
msgpack_buffer_peek_top_1(msgpack_buffer_t* b)
{
  return (int) (unsigned char) b->read_buffer[0];
}

static inline int
msgpack_buffer_read_top_1(mrb_state *mrb, msgpack_buffer_t* b)
{
  int r = (int) (unsigned char) b->read_buffer[0];

  _msgpack_buffer_consumed(mrb, b, 1);

  return r;
}

static inline int
msgpack_buffer_read_1(mrb_state *mrb, msgpack_buffer_t* b)
{
  int r;

  if (msgpack_buffer_top_readable_size(b) <= 0) {
    if (mrb_nil_p(b->io)) {
      return -1;
    }
    _msgpack_buffer_feed_from_io(mrb, b);
  }

  r = (int) (unsigned char) b->read_buffer[0];

  _msgpack_buffer_consumed(mrb, b, 1);

  return r;
}


/*
 * bulk read / skip functions
 */

size_t msgpack_buffer_read_nonblock(mrb_state *mrb, msgpack_buffer_t* b, char* buffer, size_t length);

static inline mrb_bool
msgpack_buffer_ensure_readable(mrb_state *mrb, msgpack_buffer_t* b, size_t require)
{
  if (msgpack_buffer_top_readable_size(b) < require) {
    size_t sz = msgpack_buffer_all_readable_size(b);

    if (sz < require) {
      if (mrb_bool(b->io)) {
        return FALSE;
      }
      do {
        size_t rl = _msgpack_buffer_feed_from_io(mrb, b);
        sz += rl;
      } while(sz < require);
    }
  }
  return TRUE;
}

mrb_bool _msgpack_buffer_read_all2(mrb_state *mrb, msgpack_buffer_t* b, char* buffer, size_t length);

static inline mrb_bool msgpack_buffer_read_all(mrb_state *mrb, msgpack_buffer_t* b, char* buffer, size_t length)
{
  size_t avail = msgpack_buffer_top_readable_size(b);
  if (avail < length) {
    return _msgpack_buffer_read_all2(mrb, b, buffer, length);
  }

  memcpy(buffer, b->read_buffer, length);
  _msgpack_buffer_consumed(mrb, b, length);
  return TRUE;
}

static inline size_t
msgpack_buffer_skip_nonblock(mrb_state *mrb, msgpack_buffer_t* b, size_t length)
{
  size_t avail = msgpack_buffer_top_readable_size(b);
  if (avail < length) {
    return msgpack_buffer_read_nonblock(mrb, b, NULL, length);
  }
  _msgpack_buffer_consumed(mrb, b, length);
  return length;
}

static inline union
msgpack_buffer_cast_block_t* msgpack_buffer_read_cast_block(mrb_state *mrb, msgpack_buffer_t* b, size_t n)
{
  if (!msgpack_buffer_read_all(mrb, b, b->cast_block.buffer, n)) {
    return NULL;
  }
  return &b->cast_block;
}

size_t msgpack_buffer_read_to_string_nonblock(mrb_state *mrb, msgpack_buffer_t* b, mrb_value string, size_t length);

static inline size_t
msgpack_buffer_read_to_string(mrb_state *mrb, msgpack_buffer_t* b, mrb_value string, size_t length)
{
  if (length == 0) {
    return 0;
  }

  size_t avail = msgpack_buffer_top_readable_size(b);
  if (avail > 0) {
    return msgpack_buffer_read_to_string_nonblock(mrb, b, string, length);
  } else if (!mrb_nil_p(b->io)) {
    return _msgpack_buffer_read_from_io_to_string(mrb, b, string, length);
  } else {
    return 0;
  }
}

static inline size_t
msgpack_buffer_skip(mrb_state *mrb, msgpack_buffer_t* b, size_t length)
{
  if (length == 0) {
    return 0;
  }

  size_t avail = msgpack_buffer_top_readable_size(b);
  if (avail > 0) {
    return msgpack_buffer_skip_nonblock(mrb, b, length);
  } else if (!mrb_nil_p(b->io)) {
    return _msgpack_buffer_skip_from_io(mrb, b, length);
  } else {
    return 0;
  }
}


mrb_value msgpack_buffer_all_as_string(mrb_state *mrb, msgpack_buffer_t* b);

mrb_value msgpack_buffer_all_as_string_array(mrb_state *mrb, msgpack_buffer_t* b);

static inline mrb_value
_msgpack_buffer_refer_head_mapped_string(mrb_state *mrb, msgpack_buffer_t* b, size_t length)
{
  size_t offset = b->read_buffer - b->head->first;

  return mrb_str_substr(mrb, b->head->mapped_string, offset, length);
}

static inline mrb_value
msgpack_buffer_read_top_as_string(mrb_state *mrb, msgpack_buffer_t* b, size_t length, mrb_bool frozen)
{
#ifndef DISABLE_BUFFER_READ_REFERENCE_OPTIMIZE
    /* optimize */
    if (!frozen &&
        !mrb_obj_equal(mrb, b->head->mapped_string, NO_MAPPED_STRING) &&
        length >= b->read_reference_threshold) {
        mrb_value result = _msgpack_buffer_refer_head_mapped_string(mrb, b, length);
      _msgpack_buffer_consumed(mrb, b, length);
      return result;
    }
#endif
    mrb_value result = mrb_str_new(mrb, b->read_buffer, length);
    if (frozen) {
      //puts("frozon is noimplemented.\n");
      //rb_obj_freeze(result);
    }
    _msgpack_buffer_consumed(mrb, b, length);
    return result;
}
#if defined(__cplusplus)
}  /* extern "C" { */
#endif

#endif /* MSGPACK_MRUBY_BUFFER_H__ */
