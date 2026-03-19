#!/bin/bash
# Script to start Seq in Docker for MarketData logging

echo "Starting Seq container..."

# Check if container already exists
if [ "$(docker ps -aq -f name=seq)" ]; then
    echo "Seq container already exists. Starting it..."
    docker start seq
else
    echo "Creating new Seq container..."
    docker run --name seq -d --restart unless-stopped \
      -e ACCEPT_EULA=Y \
      -p 5341:80 \
      -v seq-data:/data \
      datalust/seq:latest
fi

echo ""
echo "Seq is now running!"
echo "UI: http://localhost:5341"
echo ""
echo "To stop Seq: docker stop seq"
echo "To view logs: docker logs seq"
