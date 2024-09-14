#include <string.h>
#include <mruby.h>
#include <mruby/string.h>
#include <mruby/array.h>
#include "msgpack_buffer.h"
#include "msgpack_rmem.h"

#ifdef COMPAT_HAVE_ENCODING  /* see compat.h*/
int s_enc_ascii8bit;
#endif

#ifndef HAVE_RB_STR_REPLACE
static mrb_sym s_replace;
#endif

#ifndef DISABLE_RMEM
static msgpack_rmem_t s_rmem;
#endif

void
msgpack_buffer_static_init(mrb_state* mrb)
{
#ifndef DISABLE_RMEM
  msgpack_rmem_init(mrb, &s_rmem);
#endif
#ifndef HAVE_RB_STR_REPLACE
  s_replace = mrb_intern_lit(mrb, "replace");
#endif

#ifdef COMPAT_HAVE_ENCODING
    //s_enc_ascii8bit = rb_ascii8bit_encindex();
#endif
}

void
msgpack_buffer_static_destroy(mrb_state *mrb)
{
#ifndef DISABLE_RMEM
  msgpack_rmem_destroy(mrb, &s_rmem);
#endif
}

void
msgpack_buffer_init(msgpack_buffer_t* b)
{
  memset(b, 0, sizeof(msgpack_buffer_t));

  b->head = &b->tail;
  b->write_reference_threshold = MSGPACK_BUFFER_STRING_WRITE_REFERENCE_DEFAULT;
  b->read_reference_threshold = MSGPACK_BUFFER_STRING_READ_REFERENCE_DEFAULT;
  b->io_buffer_size = MSGPACK_BUFFER_IO_BUFFER_SIZE_DEFAULT;
  b->io = mrb_nil_value();
  b->io_buffer = mrb_nil_value();
}

static void
_msgpack_buffer_chunk_destroy(mrb_state* mrb, msgpack_buffer_chunk_t* c)
{
  if (c->mem != NULL) {
#ifndef DISABLE_RMEM
    if (!msgpack_rmem_free(mrb, &s_rmem, c->mem)) {
        mrb_free(mrb, c->mem);
    }
    /* no needs to update rmem_owner because chunks will not be
     * free()ed (left in free_list) and thus *rmem_owner is
     * always valid. */
#else
    mfree(mrb, c->mem);
#endif
  }
  c->first = NULL;
  c->last = NULL;
  c->mem = NULL;
}

void
msgpack_buffer_destroy(mrb_state* mrb, msgpack_buffer_t* b)
{
  /* head is always available */
  msgpack_buffer_chunk_t* c = b->head;
  while(c != &b->tail) {
    msgpack_buffer_chunk_t* n = c->next;
    _msgpack_buffer_chunk_destroy(mrb, c);
    mrb_free(mrb, c);
    c = n;
  }
  _msgpack_buffer_chunk_destroy(mrb, c);

  c = b->free_list;
  while(c != NULL) {
    msgpack_buffer_chunk_t* n = c->next;
    mrb_free(mrb, c);
    c = n;
  }
}

void
msgpack_buffer_mark(mrb_state* mrb, msgpack_buffer_t* b)
{
  /* head is always available */
  msgpack_buffer_chunk_t* c = b->head;
  while(c != &b->tail) {
    mrb_gc_mark_value(mrb, c->mapped_string);
    c = c->next;
  }
  mrb_gc_mark_value(mrb, c->mapped_string);

  mrb_gc_mark_value(mrb, b->io);
  mrb_gc_mark_value(mrb, b->io_buffer);

  mrb_gc_mark_value(mrb, b->owner);
}

mrb_bool
_msgpack_buffer_shift_chunk(mrb_state* mrb, msgpack_buffer_t* b)
{
  _msgpack_buffer_chunk_destroy(mrb, b->head);

  if (b->head == &b->tail) {
    /* list becomes empty. don't add head to free_list
     * because head should be always available */
      b->tail_buffer_end = NULL;
      b->read_buffer = NULL;
      return FALSE;
  }

    /* add head to free_list */
    msgpack_buffer_chunk_t* next_head = b->head->next;
    b->head->next = b->free_list;
    b->free_list = b->head;

    b->head = next_head;
    b->read_buffer = next_head->first;

    return TRUE;
}

void
msgpack_buffer_clear(mrb_state *mrb, msgpack_buffer_t* b)
{
  while (_msgpack_buffer_shift_chunk(mrb, b)) {
    ;
  }
}

size_t
msgpack_buffer_read_to_string_nonblock(mrb_state *mrb, msgpack_buffer_t* b, mrb_value string, size_t length)
{
  size_t avail = msgpack_buffer_top_readable_size(b);

#ifndef DISABLE_BUFFER_READ_REFERENCE_OPTIMIZE
  /* optimize */
  if (length <= avail && RSTRING_LEN(string) == 0 &&
    !mrb_obj_equal(mrb, b->head->mapped_string, NO_MAPPED_STRING) &&
    length >= b->read_reference_threshold) {
    mrb_value s = _msgpack_buffer_refer_head_mapped_string(mrb, b, length);
    mrb_funcall(mrb, string, "replace", 1, s);
    _msgpack_buffer_consumed(mrb, b, length);
    return length;
  }
#endif

  size_t const length_orig = length;

  while (TRUE) {
    if (length <= avail) {
      mrb_str_cat(mrb, string, b->read_buffer, length);
      _msgpack_buffer_consumed(mrb, b, length);
      return length_orig;
    }

    mrb_str_cat(mrb, string, b->read_buffer, avail);
    length -= avail;

    if (!_msgpack_buffer_shift_chunk(mrb, b)) {
        return length_orig - length;
    }

    avail = msgpack_buffer_top_readable_size(b);
  }
}

size_t
msgpack_buffer_read_nonblock(mrb_state *mrb, msgpack_buffer_t* b, char* buffer, size_t length)
{
    /* buffer == NULL means skip */
    size_t const length_orig = length;

    while (TRUE) {
      size_t avail = msgpack_buffer_top_readable_size(b);

      if (length <= avail) {
        if (buffer != NULL) {
          memcpy(buffer, b->read_buffer, length);
        }
        _msgpack_buffer_consumed(mrb, b, length);
        return length_orig;
    }

    if (buffer != NULL) {
      memcpy(buffer, b->read_buffer, avail);
      buffer += avail;
    }
    length -= avail;

    if (!_msgpack_buffer_shift_chunk(mrb, b)) {
      return length_orig - length;
    }
  }
}

size_t
msgpack_buffer_all_readable_size(const msgpack_buffer_t* b)
{
  size_t sz = msgpack_buffer_top_readable_size(b);

  if (b->head == &b->tail) {
    return sz;
  }

  msgpack_buffer_chunk_t* c = b->head->next;

  while (TRUE) {
    sz += c->last - c->first;
    if (c == &b->tail) {
      return sz;
    }
    c = c->next;
  }
}

mrb_bool
_msgpack_buffer_read_all2(mrb_state *mrb, msgpack_buffer_t* b, char* buffer, size_t length)
{
  if (!msgpack_buffer_ensure_readable(mrb, b, length)) {
    return FALSE;
  }

  msgpack_buffer_read_nonblock(mrb, b, buffer, length);
  return TRUE;
}


static inline msgpack_buffer_chunk_t*
_msgpack_buffer_alloc_new_chunk(mrb_state* mrb, msgpack_buffer_t* b)
{
  msgpack_buffer_chunk_t* reuse = b->free_list;
  if (reuse == NULL) {
    return mrb_malloc(mrb, sizeof(msgpack_buffer_chunk_t));
  }
  b->free_list = b->free_list->next;
  return reuse;
}

static inline void _msgpack_buffer_add_new_chunk(mrb_state *mrb, msgpack_buffer_t* b)
{
  if (b->head == &b->tail) {
    if (b->tail.first == NULL) {
      /* empty buffer */
      return;
    }

    msgpack_buffer_chunk_t* nc = _msgpack_buffer_alloc_new_chunk(mrb, b);

    *nc = b->tail;
    b->head = nc;
    nc->next = &b->tail;

  } else {
    /* search node before tail */
    msgpack_buffer_chunk_t* before_tail = b->head;
    while (before_tail->next != &b->tail) {
      before_tail = before_tail->next;
    }

    msgpack_buffer_chunk_t* nc = _msgpack_buffer_alloc_new_chunk(mrb, b);

#ifndef DISABLE_RMEM
#ifndef DISABLE_RMEM_REUSE_INTERNAL_FRAGMENT
  //if (b->rmem_last == b->tail_buffer_end) {
    /* reuse unused rmem space */
  //  size_t unused = b->tail_buffer_end - b->tail.last;
  //  b->rmem_last -= unused;
  //}
#endif
#endif

    /* rebuild tail */
    *nc = b->tail;
    before_tail->next = nc;
    nc->next = &b->tail;
  }
}

static inline void
_msgpack_buffer_append_reference(mrb_state* mrb, msgpack_buffer_t* b, mrb_value string)
{
  mrb_value mapped_string = mrb_str_dup(mrb, string);
#ifdef COMPAT_HAVE_ENCODING
  ENCODING_SET(mapped_string, s_enc_ascii8bit);
#endif

  _msgpack_buffer_add_new_chunk(mrb, b);

  char* data = RSTRING_PTR(string);
  size_t length = RSTRING_LEN(string);

  b->tail.first = (char*) data;
  b->tail.last = (char*) data + length;
  b->tail.mapped_string = mapped_string;
  b->tail.mem = NULL;

  /* msgpack_buffer_writable_size should return 0 for mapped chunk */
  b->tail_buffer_end = b->tail.last;

  /* consider read_buffer */
  if (b->head == &b->tail) {
    b->read_buffer = b->tail.first;
  }
}

void
_msgpack_buffer_append_long_string(mrb_state* mrb, msgpack_buffer_t* b, mrb_value string)
{
  size_t length = RSTRING_LEN(string);

  if (!mrb_nil_p(b->io)) {
    msgpack_buffer_flush(mrb, b);
    mrb_funcall_argv(mrb, b->io, b->io_write_all_method, 1, &string);
  /*} else if(!STR_DUP_LIKELY_DOES_COPY(string)) {*/
  } else if (TRUE) {
    _msgpack_buffer_append_reference(mrb, b, string);
  } else {
    msgpack_buffer_append(mrb, b, RSTRING_PTR(string), length);
  }
}

static inline void*
_msgpack_buffer_chunk_malloc(mrb_state* mrb,
                             msgpack_buffer_t* b, msgpack_buffer_chunk_t* c,
                             size_t required_size, size_t* allocated_size)
{

//#if 0
#ifndef DISABLE_RMEM
  if (required_size <= MSGPACK_RMEM_PAGE_SIZE) {
#ifndef DISABLE_RMEM_REUSE_INTERNAL_FRAGMENT
  if ((size_t)(b->rmem_end - b->rmem_last) < required_size) {
#endif
    /* alloc new rmem page */
    *allocated_size = MSGPACK_RMEM_PAGE_SIZE;
    char* buffer = msgpack_rmem_alloc(mrb, &s_rmem);
    c->mem = buffer;

    /* update rmem owner */
    b->rmem_owner = &c->mem;
    b->rmem_last = b->rmem_end = buffer + MSGPACK_RMEM_PAGE_SIZE;

    return buffer;

#ifndef DISABLE_RMEM_REUSE_INTERNAL_FRAGMENT
  } else {
    /* reuse unused rmem */
    *allocated_size = (size_t)(b->rmem_end - b->rmem_last);
    char* buffer = b->rmem_last;
    b->rmem_last = b->rmem_end;

    /* update rmem owner */
    c->mem = *b->rmem_owner;
    *b->rmem_owner = NULL;
    b->rmem_owner = &c->mem;

    return buffer;
  }
#endif
  }
#else
  if (required_size < 72) {
    required_size = 72;
  }
#endif

  // TODO alignment?
  *allocated_size = required_size;
  void* mem = mrb_malloc(mrb, required_size);
  c->mem = mem;
  return mem;
}

static inline
void* _msgpack_buffer_chunk_realloc(mrb_state *mrb, msgpack_buffer_t* b, msgpack_buffer_chunk_t* c,
                                    void* mem, size_t required_size, size_t* current_size)
{
  if (mem == NULL) {
    return _msgpack_buffer_chunk_malloc(mrb, b, c, required_size, current_size);
  }

  size_t next_size = *current_size * 2;
  while (next_size < required_size) {
    next_size *= 2;
  }
  *current_size = next_size;
  mem = mrb_realloc(mrb, mem, next_size);

  c->mem = mem;
  return mem;
}

void
_msgpack_buffer_expand(mrb_state *mrb, msgpack_buffer_t* b, const char* data, size_t length, mrb_bool flush_to_io)
{
  if (flush_to_io && !mrb_nil_p(b->io)) {
    msgpack_buffer_flush(mrb, b);
    if (msgpack_buffer_writable_size(b) >= length) {
      /* data == NULL means ensure_writable */
      if (data != NULL) {
        size_t tail_avail = msgpack_buffer_writable_size(b);
        memcpy(b->tail.last, data, length);
        b->tail.last += tail_avail;
      }
      return;
    }
  }

  /* data == NULL means ensure_writable */
  if (data != NULL) {
    size_t tail_avail = msgpack_buffer_writable_size(b);
    memcpy(b->tail.last, data, tail_avail);
    b->tail.last += tail_avail;
    data += tail_avail;
    length -= tail_avail;
  }

  size_t capacity = b->tail.last - b->tail.first;

  /* can't realloc mapped chunk or rmem page */
  if (!mrb_obj_equal(mrb, b->tail.mapped_string, NO_MAPPED_STRING)
#ifndef DISABLE_RMEM
      || capacity <= MSGPACK_RMEM_PAGE_SIZE
#endif
      ) {
    /* allocate new chunk */
    _msgpack_buffer_add_new_chunk(mrb, b);

    char* mem = _msgpack_buffer_chunk_malloc(mrb, b, &b->tail, length, &capacity);

    char* last = mem;
    if (data != NULL) {
      memcpy(mem, data, length);
      last += length;
    }

    /* rebuild tail chunk */
    b->tail.first = mem;
    b->tail.last = last;
    b->tail.mapped_string = NO_MAPPED_STRING;
    b->tail_buffer_end = mem + capacity;

    /* consider read_buffer */
    if (b->head == &b->tail) {
        b->read_buffer = b->tail.first;
    }

  } else {
    /* realloc malloc()ed chunk or NULL */
    size_t tail_filled = b->tail.last - b->tail.first;
    char* mem = _msgpack_buffer_chunk_realloc(mrb, b, &b->tail,
                                              b->tail.first, tail_filled+length, &capacity);

    char* last = mem + tail_filled;
    if (data != NULL) {
      memcpy(last, data, length);
      last += length;
    }

    /* consider read_buffer */
    if (b->head == &b->tail) {
      size_t read_offset = b->read_buffer - b->head->first;

      b->read_buffer = mem + read_offset;
    }

    /* rebuild tail chunk */
    b->tail.first = mem;
    b->tail.last = last;
    b->tail_buffer_end = mem + capacity;
  }
}

static inline mrb_value
 _msgpack_buffer_head_chunk_as_string(mrb_state* mrb, msgpack_buffer_t* b)
{
  size_t length = b->head->last - b->read_buffer;
  if (length == 0) {
    return mrb_str_buf_new(mrb, 0);
  }

  if (!mrb_obj_equal(mrb, b->head->mapped_string, NO_MAPPED_STRING)) {
    return _msgpack_buffer_refer_head_mapped_string(mrb, b, length);
  }

   return mrb_str_new(mrb, b->read_buffer, length);
}

static inline mrb_value
_msgpack_buffer_chunk_as_string(mrb_state* mrb, msgpack_buffer_chunk_t* c)
{
  size_t chunk_size = c->last - c->first;
  if (chunk_size == 0) {
    return mrb_str_buf_new(mrb, 0);
  }

  if (!mrb_obj_equal(mrb, c->mapped_string, NO_MAPPED_STRING)) {
    return mrb_str_dup(mrb, c->mapped_string);
  }

  return mrb_str_new(mrb, c->first, chunk_size);
}

mrb_value
msgpack_buffer_all_as_string(mrb_state *mrb, msgpack_buffer_t* b)
{
  if (b->head == &b->tail) {
    return _msgpack_buffer_head_chunk_as_string(mrb, b);
  }

  size_t length = msgpack_buffer_all_readable_size(b);
  mrb_value string = mrb_str_new(mrb, NULL, length);
  char* buffer = RSTRING_PTR(string);

  size_t avail = msgpack_buffer_top_readable_size(b);
  memcpy(buffer, b->read_buffer, avail);
  buffer += avail;
  length -= avail;

  msgpack_buffer_chunk_t* c = b->head->next;

  while (TRUE) {
    avail = c->last - c->first;
    memcpy(buffer, c->first, avail);

    if (length <= avail) {
      return string;
    }
    buffer += avail;
    length -= avail;

    c = c->next;
  }
}

mrb_value
msgpack_buffer_all_as_string_array(mrb_state *mrb, msgpack_buffer_t* b)
{
  if (b->head == &b->tail) {
    mrb_value s = msgpack_buffer_all_as_string(mrb, b);
    mrb_value ary = mrb_ary_new(mrb);
    mrb_ary_push(mrb, ary, s);
    return ary;
  }

  /* TODO optimize ary construction */
  //mrb_value ary = mrb_ary_new(mrb);
  mrb_value ary = mrb_nil_value();

  mrb_value s = _msgpack_buffer_head_chunk_as_string(mrb, b);
  //mrb_ary_push(ary, s);

  msgpack_buffer_chunk_t* c = b->head->next;

  while (TRUE) {
    s = _msgpack_buffer_chunk_as_string(mrb, c);
    //mrb_ary_push(mrb, ary, s);
    if (c == &b->tail) {
      return ary;
    }
    c = c->next;
  }

  return ary;
}

size_t
msgpack_buffer_flush_to_io(mrb_state* mrb, msgpack_buffer_t* b, mrb_value io, mrb_sym write_method, mrb_bool consume)
{
  if (msgpack_buffer_top_readable_size(b) == 0) {
    return 0;
  }

  mrb_value s = _msgpack_buffer_head_chunk_as_string(mrb, b);
  mrb_funcall_argv(mrb, io, write_method, 1, &s);
  size_t sz = RSTRING_LEN(s);

  if (consume) {
    while (_msgpack_buffer_shift_chunk(mrb, b)) {
      s = _msgpack_buffer_chunk_as_string(mrb, b->head);
      mrb_funcall_argv(mrb, io, write_method, 1, &s);
        sz += RSTRING_LEN(s);
      }
      return sz;

    } else {
      if (b->head == &b->tail) {
        return sz;
      }
      msgpack_buffer_chunk_t* c = b->head->next;
      while(TRUE) {
        s = _msgpack_buffer_chunk_as_string(mrb, c);
        mrb_funcall_argv(mrb, io, write_method, 1, &s);
        sz += RSTRING_LEN(s);
        if (c == &b->tail) {
          return sz;
        }
        c = c->next;
    }
  }
}

size_t
_msgpack_buffer_feed_from_io(mrb_state* mrb, msgpack_buffer_t* b)
{
  // puts("_msgpack_buffer_feed_from_io in buffer.c\n");
  size_t len;

  if (mrb_nil_p(b->io_buffer)) {
    // puts("check 1100 in buffer.c\n");
    // las argv error
    //b->io_buffer = mrb_funcall_argv(mrb, b->io, b->io_partial_read_method, 1, LONG2NUM(b->io_buffer_size));
    if (mrb_nil_p(b->io_buffer)) {
      // puts("check 1200 in buffer.c\n");
      /* errorがわからん*/
      //mrb_raise(mrb, rb_eEOFError, "IO reached end of file");
    }
    //StringValue(b->io_buffer);
  } else {
    // puts("check 1300 in buffer.c\n");
      // las argv error
    //mrb_value ret = mrb_funcall_argv(mrb, b->io, b->io_partial_read_method, 2, LONG2NUM(b->io_buffer_size), b->io_buffer);
    mrb_value ret = mrb_nil_value();
    // puts("check 1400 in buffer.c\n");
    if (mrb_nil_p(ret)) {
      /* errorがわからん*/
      //mrb_raise(mrb, rb_eEOFError, "IO reached end of file");
    }
  }

  // puts("check 1500 in buffer.c\n");

  //len = RSTRING_LEN(b->io_buffer);
  len = 0;
  if (len == 0) {
    /* errorがわからん*/
    //mrb_raise(mrb, rb_eEOFError, "IO reached end of file");
    // puts("IO reached end of file\n");
  }

  // puts("check 1600 in buffer.c\n");
  /* TODO zero-copy optimize? */
  msgpack_buffer_append_nonblock(mrb, b, RSTRING_PTR(b->io_buffer), len);

  // puts("check 1700 in buffer.c\n");

  return len;
}

size_t
_msgpack_buffer_read_from_io_to_string(mrb_state *mrb, msgpack_buffer_t* b, mrb_value string, size_t length)
{
  if (RSTRING_LEN(string) == 0) {
    /* Direct read */
    //mrb_value ret = rb_funcall_args(mrb, b->io, b->io_partial_read_method, 2, LONG2NUM(length), string);
    mrb_value ret = mrb_nil_value();
    if (mrb_nil_p(ret)) {
      return 0;
    }
    return RSTRING_LEN(string);
  }

  /* copy via io_buffer */
  if (mrb_nil_p(b->io_buffer)) {
    //b->io_buffer = mrb_str_buf_new(0);
  }

  //mrb_value ret = rb_funcall_args(mrb, b->io, b->io_partial_read_method, 2, LONG2NUM(length), b->io_buffer);
  mrb_value ret = mrb_nil_value();
  if (mrb_nil_p(ret)) {
    return 0;
  }
  size_t rl = RSTRING_LEN(b->io_buffer);

  mrb_str_buf_cat(mrb, string, (const void*)RSTRING_PTR(b->io_buffer), rl);
  return rl;
}

size_t
_msgpack_buffer_skip_from_io(mrb_state *mrb, msgpack_buffer_t* b, size_t length)
{
  mrb_value ret;

  if (mrb_nil_p(b->io_buffer)) {
    //b->io_buffer = mrb_str_buf_new(0);
  }

  //ret = mrb_funcall_argv(mrb, b->io, b->io_partial_read_method, 2, LONG2NUM(length), b->io_buffer);
  if (mrb_nil_p(ret)) {
    return 0;
  }
  return RSTRING_LEN(b->io_buffer);
}
