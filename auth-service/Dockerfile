FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["shared-kernel/EventFlow.SharedKernel/EventFlow.SharedKernel.csproj", "shared-kernel/EventFlow.SharedKernel/"]
COPY ["auth-service/EventFlow.AuthService/EventFlow.AuthService.csproj", "auth-service/EventFlow.AuthService/"]
RUN dotnet restore "auth-service/EventFlow.AuthService/EventFlow.AuthService.csproj"

COPY . .
WORKDIR "/src/auth-service/EventFlow.AuthService"
RUN dotnet build "EventFlow.AuthService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventFlow.AuthService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventFlow.AuthService.dll"]
