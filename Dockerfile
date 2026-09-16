# Multi-stage build. Restore runs with the full source present on purpose: the Web SDK only downloads the Blazor
# framework assets (Microsoft.AspNetCore.App.Internal.Assets, which provides _framework/blazor.web.js) once it can
# see the .razor files, so the usual "restore from csproj files first" layer-caching trick publishes an app with no
# Blazor script.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/ src/
RUN dotnet publish src/FaultDesk.Web/FaultDesk.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# The aspnet image listens on 8080 by default (ASPNETCORE_HTTP_PORTS).
EXPOSE 8080
ENTRYPOINT ["dotnet", "FaultDesk.Web.dll"]
