#ifndef MSGPACK_MRUBY_RMEM_H__
#define MSGPACK_MRUBY_RMEM_H__

#if defined(__cplusplus)
extern "C" {
#endif

#include <mruby.h>
#include "msgpack_sysdep.h"

#ifndef MSGPACK_RMEM_PAGE_SIZE
#define MSGPACK_RMEM_PAGE_SIZE (4*1024)
#endif

struct msgpack_rmem_t;
typedef struct msgpack_rmem_t msgpack_rmem_t;

struct msgpack_rmem_chunk_t;
typedef struct msgpack_rmem_chunk_t msgpack_rmem_chunk_t;

/*
 * a chunk contains 32 pages.
 * size of each buffer is MSGPACK_RMEM_PAGE_SIZE bytes.
 */
struct msgpack_rmem_chunk_t {
  unsigned int mask;
  char* pages;
};

struct msgpack_rmem_t {
  msgpack_rmem_chunk_t head;
  msgpack_rmem_chunk_t* array_first;
  msgpack_rmem_chunk_t* array_last;
  msgpack_rmem_chunk_t* array_end;
};

/* assert MSGPACK_RMEM_PAGE_SIZE % sysconf(_SC_PAGE_SIZE) == 0 */
void
msgpack_rmem_init(mrb_state* mrb, msgpack_rmem_t* pm);

void
msgpack_rmem_destroy(mrb_state* mrb, msgpack_rmem_t* pm);

void*
_msgpack_rmem_alloc2(mrb_state* mrb, msgpack_rmem_t* pm);

#define _msgpack_rmem_chunk_available(c) ((c)->mask != 0)

static inline void*
_msgpack_rmem_chunk_alloc(msgpack_rmem_chunk_t* c)
{
  _msgpack_bsp32(pos, c->mask);
  (c)->mask &= ~(1 << pos);
  return ((char*)(c)->pages) + (pos * (MSGPACK_RMEM_PAGE_SIZE));
}

static inline mrb_bool
_msgpack_rmem_chunk_try_free(msgpack_rmem_chunk_t* c, void* mem)
{
  ptrdiff_t pdiff = ((char*)(mem)) - ((char*)(c)->pages);
  if (0 <= pdiff && pdiff < MSGPACK_RMEM_PAGE_SIZE * 32) {
    size_t pos = pdiff / MSGPACK_RMEM_PAGE_SIZE;
    (c)->mask |= (1 << pos);
    return TRUE;
  }
  return FALSE;
}

static inline void*
msgpack_rmem_alloc(mrb_state *mrb, msgpack_rmem_t* pm)
{
  if (_msgpack_rmem_chunk_available(&pm->head)) {
    return _msgpack_rmem_chunk_alloc(&pm->head);
  }
  return _msgpack_rmem_alloc2(mrb, pm);
}

void
_msgpack_rmem_chunk_free(mrb_state *mrb, msgpack_rmem_t* pm, msgpack_rmem_chunk_t* c);

static inline mrb_bool
msgpack_rmem_free(mrb_state *mrb, msgpack_rmem_t* pm, void* mem)
{
  if (_msgpack_rmem_chunk_try_free(&pm->head, mem)) {
    return TRUE;
  }

  /* search from last */
  msgpack_rmem_chunk_t* c = pm->array_last - 1;
  msgpack_rmem_chunk_t* before_first = pm->array_first - 1;
  for (; c != before_first; c--) {
    if (_msgpack_rmem_chunk_try_free(c, mem)) {
      if (c != pm->array_first && c->mask == 0xffffffff) {
          _msgpack_rmem_chunk_free(mrb, pm, c);
      }
      return TRUE;
    }
  }
  return FALSE;
}

#if defined(__cplusplus)
}  /* extern "C" { */
#endif

#endif /* MSGPACK_MRUBY_RMEM_H__ */
