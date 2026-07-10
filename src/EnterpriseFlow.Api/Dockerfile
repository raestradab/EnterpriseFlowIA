# syntax=docker/dockerfile:1

# Build context is the repo root (see docker-compose.yml) — EnterpriseFlow.Api references
# Domain/Application/Infrastructure by ProjectReference, so `dotnet restore` needs their .csproj
# files reachable at the same relative paths they have in the solution.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Central Package Management (Directory.Packages.props) resolves PackageReference versions —
# it and stylecop.json (required by the StyleCop.Analyzers wired in Directory.Build.props, which
# also sets TreatWarningsAsErrors) must be present before any restore/build step runs.
COPY Directory.Build.props Directory.Packages.props stylecop.json ./

# csproj files only, copied first, so `dotnet restore` is cached in its own layer and doesn't
# re-run on every source change — only when a .csproj actually changes.
COPY src/EnterpriseFlow.Domain/EnterpriseFlow.Domain.csproj src/EnterpriseFlow.Domain/
COPY src/EnterpriseFlow.Application/EnterpriseFlow.Application.csproj src/EnterpriseFlow.Application/
COPY src/EnterpriseFlow.Infrastructure/EnterpriseFlow.Infrastructure.csproj src/EnterpriseFlow.Infrastructure/
COPY src/EnterpriseFlow.Api/EnterpriseFlow.Api.csproj src/EnterpriseFlow.Api/
RUN dotnet restore src/EnterpriseFlow.Api/EnterpriseFlow.Api.csproj

COPY src/EnterpriseFlow.Domain/ src/EnterpriseFlow.Domain/
COPY src/EnterpriseFlow.Application/ src/EnterpriseFlow.Application/
COPY src/EnterpriseFlow.Infrastructure/ src/EnterpriseFlow.Infrastructure/
COPY src/EnterpriseFlow.Api/ src/EnterpriseFlow.Api/

# TreatWarningsAsErrors=true (Directory.Build.props) means this step already fails the image
# build on any analyzer/StyleCop warning — the same gate CI runs, not a separate concern here.
RUN dotnet publish src/EnterpriseFlow.Api/EnterpriseFlow.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

# Matches ASPNETCORE_URLS set in docker-compose.yml — no port is hardcoded in the image itself
# beyond this documentation/metadata declaration.
EXPOSE 8080

ENTRYPOINT ["dotnet", "EnterpriseFlow.Api.dll"]
