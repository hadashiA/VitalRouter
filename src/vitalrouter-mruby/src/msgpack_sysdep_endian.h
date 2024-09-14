#ifndef MSGPACK_MRUBY_SYSDEP_ENDIAN_H__
#define MSGPACK_MRUBY_SYSDEP_ENDIAN_H__

#if defined(__cplusplus)
extern "C" {
#endif

/* including arpa/inet.h requires an extra dll on win32 */
#ifndef _WIN32
#include <arpa/inet.h>  /* __BYTE_ORDER */
#endif

/*
 * Use following command to add consitions here:
 *   cpp -dM `echo "#include <arpa/inet.h>" > test.c; echo test.c` | grep ENDIAN
 */
#if !defined(__LITTLE_ENDIAN__) && !defined(__BIG_ENDIAN__)  /* Mac OS X */
#  if defined(_LITTLE_ENDIAN) \
        || ( defined(__BYTE_ORDER) && defined(__LITTLE_ENDIAN) \
                && __BYTE_ORDER == __LITTLE_ENDIAN ) /* Linux */ \
        || ( defined(__BYTE_ORDER__) && defined(__ORDER_LITTLE_ENDIAN__) \
                && __BYTE_ORDER__ == __ORDER_LITTLE_ENDIAN__ ) /* Solaris */
#    define __LITTLE_ENDIAN__
#  elif defined(_BIG_ENDIAN) \
        || (defined(__BYTE_ORDER) && defined(__BIG_ENDIAN) \
                && __BYTE_ORDER == __BIG_ENDIAN) /* Linux */ \
        || (defined(__BYTE_ORDER__) && defined(__ORDER_BIG_ENDIAN__) \
                && __BYTE_ORDER__ == __ORDER_BIG_ENDIAN__) /* Solaris */
#    define __BIG_ENDIAN__
#  elif defined(_WIN32)  /* Win32 */
#    define __LITTLE_ENDIAN__
#  endif
#endif

#if defined(__cplusplus)
}  /* extern "C" { */
#endif

#endif /* MSGPACK_MRUBY_SYSDEP_ENDIAN_H__ */
