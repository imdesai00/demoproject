// This file's apiUrl is overwritten at Docker build time (see docker/frontend.Dockerfile,
// driven by API_BASE_URL in .env). The value below is only used for a plain `ng build`
// run outside Docker.
export const environment = {
  production: true,
  apiUrl: 'http://localhost:5000/api'
};
