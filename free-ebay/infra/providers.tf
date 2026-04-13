terraform {
    required_version = ">= 1.6.0"

    required_providers {
        aws = {
            source  = "hashicorp/aws"
            version = "~> 5.0"
        }
        tls = {
            source  = "hashicorp/tls"
            version = "~> 4.0"
        }
        local = {
            source  = "hashicorp/local"
            version = "~> 2.5"
        }
    }

    # store state remotely in S3
    backend "s3" {
        bucket         = "strokanostan-terraform-state"
        key            = "strokanostan/terraform.tfstate"
        region         = "eu-west-1"
        encrypt        = true
        dynamodb_table = "strokanostan-tf-lock"
    }
}

provider "aws" {
    region = var.aws_region
}