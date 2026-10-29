// Local-dev-only stand-in for the APISIX gateway: when running `ng serve` directly against
// the three services on their own ports (no Docker), this replicates the same routing rules
// as src/ApiGateway/apisix_conf/apisix.yaml so the frontend can talk to one origin.
//
// Angular's dev-server builder (@angular/build:dev-server) proxies through Vite's native
// proxy, not webpack-dev-server/http-proxy-middleware — so a `router` callback is NOT
// supported here. Vite matches each key either as a literal path prefix, or (when the key
// starts with "^") as a regular expression, testing object keys in insertion order and using
// the first match. The task-subpath rule MUST be listed before the general '/api/projects'
// rule so it wins before the prefix match does.
const IDENTITY = 'http://localhost:5100';
const PROJECT = 'http://localhost:5200';
const TASK = 'http://localhost:5300';

module.exports = {
  '/.well-known/jwks.json': { target: IDENTITY, secure: false },
  '/api/auth': { target: IDENTITY, secure: false },
  '/api/internal/tasks': { target: TASK, secure: false },
  '^/api/projects/[^/]+/tasks': { target: TASK, secure: false },
  '/api/dashboard': { target: PROJECT, secure: false },
  '/api/projects': { target: PROJECT, secure: false }
};
