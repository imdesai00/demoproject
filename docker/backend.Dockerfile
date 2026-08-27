FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/TaskManager.sln ./
COPY backend/src/TaskManager.Domain/TaskManager.Domain.csproj src/TaskManager.Domain/
COPY backend/src/TaskManager.Application/TaskManager.Application.csproj src/TaskManager.Application/
COPY backend/src/TaskManager.Infrastructure/TaskManager.Infrastructure.csproj src/TaskManager.Infrastructure/
COPY backend/src/TaskManager.Api/TaskManager.Api.csproj src/TaskManager.Api/
COPY backend/tests/TaskManager.UnitTests/TaskManager.UnitTests.csproj tests/TaskManager.UnitTests/
RUN dotnet restore src/TaskManager.Api/TaskManager.Api.csproj

COPY backend/src src
COPY backend/tests tests
RUN dotnet publish src/TaskManager.Api/TaskManager.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --gid 1000 appgroup && adduser --uid 1000 --gid 1000 --disabled-password appuser
COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --retries=10 --start-period=15s \
    CMD bash -c 'exec 3<>/dev/tcp/localhost/8080' || exit 1

ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]
