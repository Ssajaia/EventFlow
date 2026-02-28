FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["shared-kernel/EventFlow.SharedKernel/EventFlow.SharedKernel.csproj", "shared-kernel/EventFlow.SharedKernel/"]
COPY ["worker-service/EventFlow.WorkerService/EventFlow.WorkerService.csproj", "worker-service/EventFlow.WorkerService/"]
RUN dotnet restore "worker-service/EventFlow.WorkerService/EventFlow.WorkerService.csproj"
COPY . .
WORKDIR "/src/worker-service/EventFlow.WorkerService"
RUN dotnet build "EventFlow.WorkerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventFlow.WorkerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventFlow.WorkerService.dll"]
