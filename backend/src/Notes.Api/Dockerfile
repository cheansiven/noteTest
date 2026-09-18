# Build context is the repository ROOT (see docker-compose.yml): the infrastructure
# project embeds database/schema.sql, which lives outside backend/.
#   docker build -f backend/src/Notes.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore on its own layer so code edits do not re-download packages.
COPY backend/Directory.Build.props backend/
COPY backend/src/Notes.Domain/Notes.Domain.csproj                 backend/src/Notes.Domain/
COPY backend/src/Notes.Application/Notes.Application.csproj       backend/src/Notes.Application/
COPY backend/src/Notes.Infrastructure/Notes.Infrastructure.csproj backend/src/Notes.Infrastructure/
COPY backend/src/Notes.Api/Notes.Api.csproj                       backend/src/Notes.Api/
RUN dotnet restore backend/src/Notes.Api/Notes.Api.csproj

COPY backend/src/ backend/src/
COPY database/ database/
RUN dotnet publish backend/src/Notes.Api/Notes.Api.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is only here so the container can report its own health to Compose.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Run as the non-root user the .NET base images provide.
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "Notes.Api.dll"]
