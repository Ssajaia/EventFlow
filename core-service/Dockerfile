FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["shared-kernel/EventFlow.SharedKernel/EventFlow.SharedKernel.csproj", "shared-kernel/EventFlow.SharedKernel/"]
COPY ["core-service/EventFlow.CoreService/EventFlow.CoreService.csproj", "core-service/EventFlow.CoreService/"]
RUN dotnet restore "core-service/EventFlow.CoreService/EventFlow.CoreService.csproj"
COPY . .
WORKDIR "/src/core-service/EventFlow.CoreService"
RUN dotnet build "EventFlow.CoreService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventFlow.CoreService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventFlow.CoreService.dll"]
