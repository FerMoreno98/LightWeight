FROM oven/bun:1.4.4 AS build

WORKDIR /app

COPY ./package.json ./bun.lock ./

RUN bun install

COPY . ./

RUN bun run build


FROM nginx:stable-alpine AS runtime
WORKDIR /usr/share/nginx/html
COPY --from=build /app/dist/LightWeight/browser .
COPY ./nginx.conf /etc/nginx/conf.d/default.conf