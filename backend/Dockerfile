# syntax=docker/dockerfile:1

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG TARGETARCH

WORKDIR /src
COPY . .

WORKDIR /src/src/RoadMate

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet publish -a ${TARGETARCH/amd64/x64} \
  --use-current-runtime \
  --self-contained false \
  -o /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS development

WORKDIR /src
COPY . .

WORKDIR /src/src/RoadMate

CMD ["dotnet", "run", "--no-launch-profile"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

WORKDIR /app
COPY --from=build /app .

ARG UID=10001

RUN adduser \
  --disabled-password \
  --gecos "" \
  --home "/nonexistent" \
  --shell "/sbin/nologin" \
  --no-create-home \
  --uid "${UID}" \
  appuser

USER appuser

ENTRYPOINT ["dotnet", "RoadMate.dll"]