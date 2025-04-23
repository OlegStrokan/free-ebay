#!/bin/bash

# Get the application pod name
APP_POD=$(kubectl get pods -n free-ebay -l app=free-ebay -o jsonpath='{.items[0].metadata.name}' 2>/dev/null)
if [ -z "$APP_POD" ]; then
  echo "No application pods found. Make sure the deployment is running."
  exit 1
fi

echo "Application pod: $APP_POD"

# Check if the database is accessible from the application pod
echo "Checking if the database is accessible from the application pod..."
kubectl exec -n free-ebay $APP_POD -- sh -c "nc -zv postgres-svc 5432"

# Check the environment variables in the application pod
echo "Checking environment variables in the application pod..."
kubectl exec -n free-ebay $APP_POD -- sh -c "env | grep DB_"

# Check if the database.datasource.js file exists and its content
echo "Checking database.datasource.js file..."
kubectl exec -n free-ebay $APP_POD -- sh -c "cat /app/dist/shared/database/database.datasource.js | grep -A 10 'host:'"
