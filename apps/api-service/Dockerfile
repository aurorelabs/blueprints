# syntax=docker/dockerfile:1

# Build context: the folder that holds foundation/ and blueprints/ side by side (the foundation is referenced by path for now).
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS restore
WORKDIR /src

COPY foundation/global.json foundation/Directory.Build.props foundation/Versions.props foundation/
COPY --parents foundation/src/*/*.csproj ./
COPY blueprints/apps/api-service/global.json blueprints/apps/api-service/Directory.Build.props blueprints/apps/api-service/Blueprints.ApiService.slnx blueprints/apps/api-service/
COPY --parents blueprints/apps/api-service/src/*/*.csproj blueprints/apps/api-service/tests/*/*.csproj blueprints/apps/api-service/tools/*/*.csproj ./
RUN dotnet restore blueprints/apps/api-service/Blueprints.ApiService.slnx

FROM restore AS build
COPY foundation/ foundation/
COPY blueprints/apps/api-service/ blueprints/apps/api-service/

FROM build AS test
WORKDIR /src/blueprints/apps/api-service
RUN dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.*
ENV PATH="$PATH:/root/.dotnet/tools"

COPY <<'SCRIPT' run-tests.sh
#!/usr/bin/env bash
set -euo pipefail

for project in Tests Tests.Features Tests.Integration; do
    dotnet test "tests/$project" --configuration Release \
        --coverage --coverage-output-format cobertura --coverage-output "$project.cobertura.xml"
done

reportgenerator \
    "-reports:artifacts/test-results/*.cobertura.xml" \
    "-targetdir:artifacts/coverage-report" \
    "-reporttypes:Html;TextSummary" \
    "-assemblyfilters:+Blueprints.ApiService;+Aurore.Blueprints.*;-Aurore.Blueprints.ApiService.Tools.Migrations"

cat artifacts/coverage-report/Summary.txt
SCRIPT
RUN chmod +x run-tests.sh
ENTRYPOINT ["./run-tests.sh"]

FROM build AS publish
RUN dotnet publish blueprints/apps/api-service/src/App.Api/App.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
WORKDIR /app

COPY --from=publish /app .

EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "Blueprints.ApiService.dll"]
