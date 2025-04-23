#!/bin/bash

# Exit on error
set -e

echo "=== Starting Kubernetes Cleanup ==="

# Delete the free-ebay namespace and all resources in it
echo "Deleting the free-ebay namespace and all resources in it..."
kubectl delete namespace free-ebay --ignore-not-found=true

# Delete any persistent volumes related to the project
echo "Deleting persistent volumes..."
kubectl get pv | grep free-ebay | awk '{print $1}' | xargs -r kubectl delete pv

# Delete any persistent volume claims related to the project
echo "Deleting persistent volume claims..."
kubectl get pvc | grep free-ebay | awk '{print $1}' | xargs -r kubectl delete pvc

# Delete any ConfigMaps related to the project
echo "Deleting ConfigMaps..."
kubectl get configmap | grep free-ebay | awk '{print $1}' | xargs -r kubectl delete configmap

# Delete any Secrets related to the project
echo "Deleting Secrets..."
kubectl get secret | grep free-ebay | awk '{print $1}' | xargs -r kubectl delete secret

# Delete any Services related to the project
echo "Deleting Services..."
kubectl get svc | grep free-ebay | awk '{print $1}' | xargs -r kubectl delete svc

# Delete any Deployments related to the project
echo "Deleting Deployments..."
kubectl get deploy | grep free-ebay | awk '{print $1}' | xargs -r kubectl delete deploy

# Delete any StatefulSets related to the project
echo "Deleting StatefulSets..."
kubectl get sts | grep free-ebay | awk '{print $1}' | xargs -r kubectl delete sts

# Delete any DaemonSets related to the project
echo "Deleting DaemonSets..."
kubectl get ds | grep free-ebay | awk '{print $1}' | xargs -r kubectl delete ds

# Delete any Jobs related to the project
echo "Deleting Jobs..."
kubectl get jobs | grep free-ebay | awk '{print $1}' | xargs -r kubectl delete jobs

# Delete any CronJobs related to the project
echo "Deleting CronJobs..."
kubectl get cronjobs | grep free-ebay | awk '{print $1}' | xargs -r kubectl delete cronjobs

echo "=== Kubernetes Cleanup Complete ==="
