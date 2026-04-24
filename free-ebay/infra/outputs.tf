output "eks_cluster_name" {
    description = "EKS cluster name — pass to kubectl/helm"
    value = aws_eks_cluster.main.name
}

output "eks_cluster_endpoint" {
    description = "EKS API server endpoint"
    value = aws_eks_cluster.main.endpoint
    sensitive = true
}

output "eks_cluster_ca" {
    description = "EKS cluster CA certificate (base64)"
    value = aws_eks_cluster.main.certificate_authority[0].data
    sensitive = true
}

output "ecr_registry" {
    description = "ECR registry hostname — prefix all image tags with this"
    value = "${data.aws_caller_identity.current.account_id}.dkr.ecr.${var.aws_region}.amazonaws.com"
}

output "ecr_repository_urls" {
    description = "Full ECR repository URLs for each service"
    value = { for k, v in aws_ecr_repository.services : k => v.repository_url }
}

output "vpc_id" {
    description = "VPC ID"
    value = aws_vpc.main.id
}

output "private_subnet_ids" {
    description = "Private subnet IDs (EKS nodes)"
    value = aws_subnet.private[*].id
}

output "public_subnet_ids" {
    description = "Public subnet IDs (ALB / NAT)"
    value = aws_subnet.public[*].id
}

output "kubeconfig_command" {
    description = "Run this after terraform apply to configure kubectl"
    value = "aws eks update-kubeconfig --region ${var.aws_region} --name ${aws_eks_cluster.main.name}"
}
