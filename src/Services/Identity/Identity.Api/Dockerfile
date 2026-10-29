FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TaskFlow.sln ./
COPY src/BuildingBlocks/Common.Data/Common.Data.csproj src/BuildingBlocks/Common.Data/
COPY src/BuildingBlocks/Common.Web/Common.Web.csproj src/BuildingBlocks/Common.Web/
COPY src/BuildingBlocks/Common.Security/Common.Security.csproj src/BuildingBlocks/Common.Security/
COPY src/BuildingBlocks/Common.Messaging/Common.Messaging.csproj src/BuildingBlocks/Common.Messaging/
COPY src/Services/Identity/Identity.Api/Identity.Api.csproj src/Services/Identity/Identity.Api/
RUN dotnet restore src/Services/Identity/Identity.Api/Identity.Api.csproj

COPY src/BuildingBlocks src/BuildingBlocks
COPY src/Services/Identity/Identity.Api src/Services/Identity/Identity.Api
RUN dotnet publish src/Services/Identity/Identity.Api/Identity.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --gid 1000 appgroup && adduser --uid 1000 --gid 1000 --disabled-password appuser
COPY --from=build /app/publish .
RUN mkdir -p /app/keys && chown -R appuser:appgroup /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --retries=10 --start-period=15s \
    CMD bash -c 'exec 3<>/dev/tcp/localhost/8080' || exit 1

ENTRYPOINT ["dotnet", "Identity.Api.dll"]
