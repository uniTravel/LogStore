FROM mcr.microsoft.com/dotnet/core/runtime:3.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-alpine AS build
WORKDIR /src/LogStore.Messaging/Broker
COPY LogStore.Messaging.Broker.fsproj .
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "LogStore.Messaging.Broker.dll"]