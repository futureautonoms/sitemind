FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["sitemind-shared/sitemind-shared.csproj", "sitemind-shared/"]
COPY ["sitemind-ingestion/sitemind-ingestion.csproj", "sitemind-ingestion/"]
RUN dotnet restore "sitemind-ingestion/sitemind-ingestion.csproj"
COPY . .
WORKDIR "/src/sitemind-ingestion"
RUN dotnet build "sitemind-ingestion.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "sitemind-ingestion.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "sitemind-ingestion.dll"]

