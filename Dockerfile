# ThermoCore Web (Blazor) + in-process application services — Linux container image
# Build: docker build -t thermocore-web .
# Run:   docker run --rm -p 8080:8080 thermocore-web

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ThermoCore.sln ./
COPY global.json ./
COPY src/ThermoCore.Core/ThermoCore.Core.csproj src/ThermoCore.Core/
COPY src/ThermoCore.AWG/ThermoCore.AWG.csproj src/ThermoCore.AWG/
COPY src/ThermoCore.Application/ThermoCore.Application.csproj src/ThermoCore.Application/
COPY src/ThermoCore.Api/ThermoCore.Api.csproj src/ThermoCore.Api/
COPY src/ThermoCore.Web/ThermoCore.Web.csproj src/ThermoCore.Web/
COPY src/ThermoCore.Console/ThermoCore.Console.csproj src/ThermoCore.Console/
COPY tests/ThermoCore.Core.Tests/ThermoCore.Core.Tests.csproj tests/ThermoCore.Core.Tests/
COPY tests/ThermoCore.AWG.Tests/ThermoCore.AWG.Tests.csproj tests/ThermoCore.AWG.Tests/
COPY tests/ThermoCore.Api.Tests/ThermoCore.Api.Tests.csproj tests/ThermoCore.Api.Tests/
COPY tests/ThermoCore.Web.Tests/ThermoCore.Web.Tests.csproj tests/ThermoCore.Web.Tests/
COPY tests/ThermoCore.IntegrationTests/ThermoCore.IntegrationTests.csproj tests/ThermoCore.IntegrationTests/

RUN dotnet restore ThermoCore.sln

COPY src/ src/
COPY tests/ tests/
COPY samples/ samples/
COPY docs/ docs/

RUN dotnet publish src/ThermoCore.Web/ThermoCore.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ThermoCore.Web.dll"]
