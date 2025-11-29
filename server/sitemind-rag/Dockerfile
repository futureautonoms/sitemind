FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["sitemind-shared/sitemind-shared.csproj", "sitemind-shared/"]
COPY ["sitemind-rag/sitemind-rag.csproj", "sitemind-rag/"]
RUN dotnet restore "sitemind-rag/sitemind-rag.csproj"
COPY . .
WORKDIR "/src/sitemind-rag"
RUN dotnet build "sitemind-rag.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "sitemind-rag.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "sitemind-rag.dll"]

