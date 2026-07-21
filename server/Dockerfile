# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TaskFlow.sln Directory.Build.props .
COPY src/TaskFlow.Domain/TaskFlow.Domain.csproj src/TaskFlow.Domain/
COPY src/TaskFlow.Application/TaskFlow.Application.csproj src/TaskFlow.Application/
COPY src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj src/TaskFlow.Infrastructure/
COPY src/TaskFlow.Api/TaskFlow.Api.csproj src/TaskFlow.Api/
COPY tests/TaskFlow.UnitTests/TaskFlow.UnitTests.csproj tests/TaskFlow.UnitTests/
COPY tests/TaskFlow.IntegrationTests/TaskFlow.IntegrationTests.csproj tests/TaskFlow.IntegrationTests/

RUN dotnet restore src/TaskFlow.Api/TaskFlow.Api.csproj

COPY . .
RUN dotnet publish src/TaskFlow.Api/TaskFlow.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Render injects $PORT at container start; ASPNETCORE_URLS must be built from it
# at runtime (a literal env var value can't reference $PORT), hence the shell entrypoint.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet TaskFlow.Api.dll"]
