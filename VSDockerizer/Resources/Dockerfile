FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS base

RUN apt update && \
    apt install unzip && \
    curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v vs2019 -l /remote_debugger

FROM base AS build

WORKDIR /source
COPY . .
RUN dotnet build -o /app

FROM build

WORKDIR /app
ENTRYPOINT ["tail", "-f", "/dev/null"]