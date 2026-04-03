# ── Build stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.config ./
COPY ClientContactManager/ClientContactManager.csproj ClientContactManager/
RUN dotnet restore ClientContactManager/ClientContactManager.csproj

COPY ClientContactManager/ ClientContactManager/
WORKDIR /src/ClientContactManager
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ── Runtime stage ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ClientContactManager.dll"]
