#!/bin/bash

set -e

echo "🚀 Starting SpeakHalo deployment..."

# Load environment variables
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
fi

# Build Docker images
echo "📦 Building Docker images..."
docker-compose build --no-cache

# Stop existing containers
echo "🛑 Stopping existing containers..."
docker-compose down

# Start services
echo "▶️  Starting services..."
docker-compose up -d

# Wait for services to be healthy
echo "⏳ Waiting for services to start..."
sleep 10

# Check health
echo "🏥 Checking health status..."
curl -f http://localhost:8080/health || exit 1

echo "✅ Deployment completed successfully!"
echo "🌐 API is running at http://localhost:8080"
echo "📊 Swagger UI: http://localhost:8080/swagger"
echo "📈 Hangfire Dashboard: http://localhost:8080/hangfire"
