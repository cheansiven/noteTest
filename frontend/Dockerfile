FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Vite inlines env vars at build time. Default "/api" keeps the API same-origin
# behind nginx, so the browser never needs a CORS pre-flight.
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

# Runs vue-tsc as well, so a type error fails the image build.
RUN npm run build

# ---------------------------------------------------------------------------
# Development image: the source is bind-mounted from the host and Vite serves it
# with hot module replacement. node_modules stays in a volume so the container's
# Linux-native binaries are not shadowed by the host's macOS ones.
# ---------------------------------------------------------------------------
FROM node:24-alpine AS develop
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

EXPOSE 5273

CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]

FROM nginx:alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
