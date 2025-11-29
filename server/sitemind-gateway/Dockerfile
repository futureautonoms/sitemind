FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["sitemind-gateway/sitemind-gateway.csproj", "sitemind-gateway/"]
RUN dotnet restore "sitemind-gateway/sitemind-gateway.csproj"
COPY . .
WORKDIR "/src/sitemind-gateway"
RUN dotnet build "sitemind-gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "sitemind-gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "sitemind-gateway.dll"]

