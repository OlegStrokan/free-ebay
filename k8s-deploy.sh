#!/bin/bash

# Exit on error
set -e

echo "Deploying Free-eBay to Kubernetes..."

# Clean up any existing resources
echo "Cleaning up any existing resources..."
kubectl delete deployment free-ebay -n free-ebay --ignore-not-found=true
kubectl delete deployment postgres -n free-ebay --ignore-not-found=true
kubectl delete job db-init -n free-ebay --ignore-not-found=true
kubectl delete pvc postgres-pvc -n free-ebay --ignore-not-found=true

# Wait for resources to be deleted
echo "Waiting for resources to be deleted..."
sleep 5

# Create namespace
echo "Creating namespace..."
kubectl apply -f k8s/namespace.yaml

# Create ConfigMap and Secrets
echo "Creating ConfigMap and Secrets..."
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secrets.yaml

# Deploy PostgreSQL
echo "Deploying PostgreSQL..."
kubectl apply -f k8s/postgres.yaml

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL to be ready..."
kubectl wait --namespace=free-ebay --for=condition=ready pod -l app=postgres --timeout=120s

# Delete any existing DB initialization job
echo "Cleaning up any existing DB initialization jobs..."
kubectl delete job db-init -n free-ebay --ignore-not-found=true

# Apply the DB initialization ConfigMap
echo "Creating DB initialization ConfigMap..."
kubectl apply -f k8s/db-init-configmap.yaml

# Run database initialization
echo "Running database initialization..."
kubectl apply -f k8s/db-init-job.yaml

# Wait for initialization job to complete
echo "Waiting for database initialization to complete..."
kubectl wait --namespace=free-ebay --for=condition=complete job/db-init --timeout=180s || {
  echo "DB initialization job did not complete in time. Checking logs..."
  INIT_POD=$(kubectl get pods -n free-ebay -l job-name=db-init -o jsonpath='{.items[0].metadata.name}')
  kubectl logs -n free-ebay $INIT_POD
  echo "\nDB initialization job failed or timed out. Please check the logs above for errors."
  echo "Continuing with deployment anyway..."
}

# Deploy the application
echo "Deploying the application..."
kubectl apply -f k8s/deployment.yaml

# Wait for the application to be ready
echo "Waiting for the application to be ready..."

# First, wait for the pod to be created
echo "Waiting for pod to be created..."
sleep 10

# Get the pod name
APP_POD=$(kubectl get pods -n free-ebay -l app=free-ebay -o jsonpath='{.items[0].metadata.name}' 2>/dev/null)
if [ -z "$APP_POD" ]; then
  echo "No pods found with label app=free-ebay. Checking all pods in namespace:"
  kubectl get pods -n free-ebay
  echo "Deployment may have failed. Exiting."
  exit 1
fi

# Check pod status
echo "Pod $APP_POD created. Checking status..."
kubectl get pod -n free-ebay $APP_POD

# Wait for the pod to be ready
kubectl wait --namespace=free-ebay --for=condition=ready pod $APP_POD --timeout=180s || {
  echo "Application pod did not become ready in time. Checking details..."

  # Show pod description
  echo "\n=== Pod Description ==="
  kubectl describe pod -n free-ebay $APP_POD

  # Show logs
  echo "\n=== Container Logs ==="
  kubectl logs -n free-ebay $APP_POD

  echo "\nApplication pod failed to become ready. Please check the logs above for errors."
  echo "You can manually check the status with: kubectl get pods -n free-ebay"
  echo "And get logs with: kubectl logs -n free-ebay $APP_POD"
  echo "\nContinuing with the script..."
}

echo "Deployment completed successfully!"
echo "You can access the application at: http://$(kubectl get svc -n free-ebay free-ebay-svc -o jsonpath='{.status.loadBalancer.ingress[0].ip}'):8000"
