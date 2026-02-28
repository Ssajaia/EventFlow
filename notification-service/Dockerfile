FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["shared-kernel/EventFlow.SharedKernel/EventFlow.SharedKernel.csproj", "shared-kernel/EventFlow.SharedKernel/"]
COPY ["notification-service/EventFlow.NotificationService/EventFlow.NotificationService.csproj", "notification-service/EventFlow.NotificationService/"]
RUN dotnet restore "notification-service/EventFlow.NotificationService/EventFlow.NotificationService.csproj"
COPY . .
WORKDIR "/src/notification-service/EventFlow.NotificationService"
RUN dotnet build "EventFlow.NotificationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventFlow.NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventFlow.NotificationService.dll"]
