FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

RUN apt-get update \
    && apt-get install -y --no-install-recommends libkrb5-3 libgssapi-krb5-2 libjemalloc2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /Mnema

RUN mkdir /files
COPY _output/*.tar.gz /files/

ARG TARGETPLATFORM
RUN set -eux; \
    case "$TARGETPLATFORM" in \
      "linux/amd64")   RID=linux-x64   ;; \
      "linux/arm64")   RID=linux-arm64 ;; \
      "linux/arm/v7")  RID=linux-arm   ;; \
      *) echo "Unsupported platform: $TARGETPLATFORM" >&2; exit 1 ;; \
    esac; \
    tar xzf "/files/mnema-${RID}.tar.gz" -C /Mnema --strip-components=1

COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 8080

ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT [ "/bin/bash" ]
CMD ["/entrypoint.sh"]
