#! /bin/bash

# https://www.libvips.org/API/current/developer-checklist.html#linux-memory-allocator
JEMALLOC_PATH=$(ldconfig -p | grep -m1 'libjemalloc.so.2' | awk '{print $NF}')
if [ -n "$JEMALLOC_PATH" ]; then
    export LD_PRELOAD="$JEMALLOC_PATH"
else
    echo "jemalloc not found, using default allocator. This may cause increased memory usage"
fi

exec ./Mnema
