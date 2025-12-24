FROM node:20-alpine AS builder
WORKDIR /app
ENV NEXT_TELEMETRY_DISABLED=1

COPY pnpm-workspace.yaml ./pnpm-workspace.yaml
COPY apps/web ./apps/web

RUN corepack enable && cd apps/web && pnpm install --frozen-lockfile=false
RUN cd apps/web && pnpm run build

FROM node:20-alpine AS runner
WORKDIR /app/apps/web
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN corepack enable && apk add --no-cache wget
COPY --from=builder /app/apps/web/.next ./.next
COPY --from=builder /app/apps/web/public ./public
COPY --from=builder /app/apps/web/package.json ./package.json
COPY --from=builder /app/apps/web/node_modules ./node_modules

EXPOSE 3000
CMD ["pnpm", "start"]
