# Build context: the client/ folder.
FROM oven/bun:1 AS deps
WORKDIR /app
COPY package.json bun.lock ./
RUN bun install --frozen-lockfile --production

FROM oven/bun:1
WORKDIR /app
ENV NODE_ENV=production \
    PORT=3000 \
    API_URL=http://server:8080

COPY --from=deps /app/node_modules ./node_modules
COPY package.json bunfig.toml tsconfig.json ./
COPY src ./src

USER bun
EXPOSE 3000
CMD ["bun", "src/index.ts"]
