FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["gateway-api/EventFlow.GatewayApi/EventFlow.GatewayApi.csproj", "gateway-api/EventFlow.GatewayApi/"]
RUN dotnet restore "gateway-api/EventFlow.GatewayApi/EventFlow.GatewayApi.csproj"
COPY . .
WORKDIR "/src/gateway-api/EventFlow.GatewayApi"
RUN dotnet build "EventFlow.GatewayApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventFlow.GatewayApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventFlow.GatewayApi.dll"]
