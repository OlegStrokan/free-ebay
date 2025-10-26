# ---------------------------
# 1. Base stage (deps cache)
# ---------------------------
    FROM node:22-alpine AS base

    WORKDIR /app
    
    # Copy only dependency files first (for cache efficiency)
    COPY package.json yarn.lock ./
    
    # ---------------------------
    # 2. Build stage
    # ---------------------------
    FROM base AS build
    
    # Install deps including dev (need build tools if native deps!)
    RUN yarn install --frozen-lockfile
    
    COPY . .
    RUN yarn build
    
    # ---------------------------
    # 3. Production deps stage
    # ---------------------------
    FROM base AS deps
    
    # Install only production deps
    RUN yarn install --production --frozen-lockfile
    
    # ---------------------------
    # 4. Runtime stage
    # ---------------------------
    FROM node:22-alpine AS runtime
    
    WORKDIR /app
    ENV NODE_ENV=production
    
    # Copy production deps
    COPY --from=deps /app/node_modules ./node_modules
    
    # Copy built app
    COPY --from=build /app/dist ./dist
    COPY --from=build /app/proto ./proto

    

    EXPOSE 3000
    CMD ["node", "dist/main.js"]
    