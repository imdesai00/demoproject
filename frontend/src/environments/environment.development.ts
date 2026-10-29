export const environment = {
  production: false,
  // Relative, so it goes through ng serve's own dev-server proxy (proxy.conf.js) — which
  // replicates the same gateway routing (api/projects/*/tasks -> Task.Api, everything else
  // under /api/projects -> Project.Api, etc.) against the three services running locally.
  apiUrl: '/api'
};
