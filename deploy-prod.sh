#!/bin/bash

# Exit on error
set -e

# Load environment variables from .env file if it exists
if [ -f .env ]; then
  export $(cat .env | grep -v '^#' | xargs)
fi

# Check if we're deploying a specific version
VERSION=${1:-latest}
echo "Deploying version: $VERSION"

# Determine which docker compose command to use
if command -v docker-compose &> /dev/null; then
  DOCKER_COMPOSE="docker-compose"
else
  DOCKER_COMPOSE="docker compose"
fi

echo "Using command: $DOCKER_COMPOSE"

# Build and start the production containers
cd core
$DOCKER_COMPOSE -f docker-compose.prod.yml build
$DOCKER_COMPOSE -f docker-compose.prod.yml up -d

# Run database migrations
echo "Running database migrations..."
$DOCKER_COMPOSE -f docker-compose.prod.yml exec app yarn migration:run

echo "Deployment completed successfully!"
echo "The application is now running at http://localhost:3000"
echo "The payment service is running at http://localhost:5012"
