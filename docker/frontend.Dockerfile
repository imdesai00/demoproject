FROM node:22-alpine AS build
WORKDIR /app

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ .

# Bake the backend's API URL into the production build. Override at build time
# via docker-compose's API_BASE_URL (.env) if your backend lives somewhere else.
ARG API_URL=http://localhost:5000/api
RUN printf "export const environment = {\n  production: true,\n  apiUrl: '%s'\n};\n" "$API_URL" > src/environments/environment.ts

RUN npm run build -- --configuration production

FROM node:22-alpine AS runtime
WORKDIR /app

RUN npm install -g serve@14
COPY --from=build /app/dist/frontend/browser ./browser

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=3s --retries=5 CMD wget -q -O /dev/null http://localhost:80/ || exit 1

# -s enables SPA fallback (unknown paths serve index.html, needed for Angular's client-side routing)
CMD ["serve", "-s", "browser", "-l", "80"]
