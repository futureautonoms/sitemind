FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["sitemind-auth/sitemind-auth.csproj", "sitemind-auth/"]
RUN dotnet restore "sitemind-auth/sitemind-auth.csproj"
COPY . .
WORKDIR "/src/sitemind-auth"
RUN dotnet build "sitemind-auth.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "sitemind-auth.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "sitemind-auth.dll"]

