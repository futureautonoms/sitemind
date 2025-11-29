# Build stage
FROM node:20-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source files
COPY . .

# Build the application
RUN npm run build

# Final stage: Copy build output to volume
FROM alpine:latest

RUN apk add --no-cache bash

WORKDIR /app

# Copy built files from build stage
COPY --from=build /app/dist /app/dist

# Create script to copy files to volume
RUN echo '#!/bin/bash' > /app/copy-dist.sh && \
    echo 'cp -r /app/dist/* /usr/share/nginx/html/' >> /app/copy-dist.sh && \
    echo 'tail -f /dev/null' >> /app/copy-dist.sh && \
    chmod +x /app/copy-dist.sh

CMD ["/app/copy-dist.sh"]

