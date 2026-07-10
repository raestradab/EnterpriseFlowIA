# syntax=docker/dockerfile:1

# Build context is this folder (src/EnterpriseFlow.Web) — self-contained, no cross-project
# references like the Api's Dockerfile needs.

FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./

# --legacy-peer-deps: documented debt (docs/08-frontend.md) — vue-router@5's peerDependencies
# want a newer Vite than the ^6.3.5 pinned here (needed to work around a Rolldown/native-binding
# issue on some Node versions). Kept for consistency with local dev, not specific to Docker.
RUN npm install --legacy-peer-deps

COPY . .

# npm run build = vue-tsc -b (typecheck) && vite build — fails the image build on a type error,
# the same gate CI runs.
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
