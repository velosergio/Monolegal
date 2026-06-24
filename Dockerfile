# =============================================================================
# Etapa 1: Frontend build
# =============================================================================
FROM node:22-alpine AS frontend-build

WORKDIR /app/frontend

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ .
RUN npm run build

# =============================================================================
# Etapa 2: .NET build (backend + worker comparten restore y código fuente)
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build

WORKDIR /app

# Copiar manifiestos primero para cachear el restore
COPY global.json .
COPY packages/shared/shared.csproj                    ./packages/shared/
COPY backend/backend.csproj                           ./backend/
COPY backend/Domain/Domain.csproj                     ./backend/Domain/
COPY backend/Application/Application.csproj           ./backend/Application/
COPY backend/Infrastructure/Infrastructure.csproj     ./backend/Infrastructure/
COPY backend/Api/Api.csproj                           ./backend/Api/
COPY worker/worker.csproj                             ./worker/

RUN dotnet restore backend/backend.csproj && \
    dotnet restore worker/worker.csproj

# Copiar código fuente
COPY packages/shared/ ./packages/shared/
COPY backend/         ./backend/
COPY worker/          ./worker/

# Publicar ambos proyectos
RUN dotnet publish backend/Api/Api.csproj -c Release -o ./publish/backend && \
    dotnet publish worker/worker.csproj   -c Release -o ./publish/worker

# =============================================================================
# Etapa 3: Base runtime compartida (usuario no-root)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime-base
# El usuario/grupo 'app' ya existe en la imagen base aspnet:10.0

# =============================================================================
# Etapa 4: Runtime backend
# =============================================================================
FROM runtime-base AS backend

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=dotnet-build   /app/publish/backend    .
COPY --from=frontend-build /app/frontend/dist      ./wwwroot

RUN chown -R app:app /app
USER app

HEALTHCHECK --interval=10s --timeout=5s --retries=5 --start-period=15s \
  CMD curl -fs http://localhost:5000/health || exit 1

EXPOSE 5000
ENTRYPOINT ["dotnet", "Api.dll"]

# =============================================================================
# Etapa 5: Runtime worker
# =============================================================================
FROM runtime-base AS worker

WORKDIR /app

COPY --from=dotnet-build /app/publish/worker .

RUN chown -R app:app /app
USER app

ENTRYPOINT ["dotnet", "worker.dll"]

# =============================================================================
# Etapa 6: Frontend dev server
# =============================================================================
FROM node:22-alpine AS frontend

WORKDIR /app

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ .

EXPOSE 5173
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]
