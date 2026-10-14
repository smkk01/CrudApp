# Deployment notes

## Frontend hosting

AWS Amplify Hosting is configured through the repository's `amplify.yml` for the Vite frontend. Connect the GitHub `main` branch in Amplify Console, set the four `VITE_*` variables documented in the README, and configure an SPA rewrite to `/index.html`.

After Amplify provides the branch domain, redeploy CDK with `FRONTEND_ORIGIN=https://main.<app-id>.amplifyapp.com` so API Gateway responses and preflight requests allow that exact origin.

## Backend hosting

The .NET API is packaged for AWS Lambda through the `Amazon.Lambda.AspNetCoreServer.Hosting` package. The `AddAWSLambdaHosting` registration is enabled in the API startup configuration.

The deployed Lambda uses the `PRODUCT_TABLE` environment variable to select the DynamoDB-backed repository. The CDK stack grants the function read/write access to that table and configures both `PRODUCT_TABLE` and `DynamoDb__TableName`.

API Gateway exposes `/health` without authentication and protects all `/api/products` methods with a Cognito user-pool authorizer. The React app obtains an ID token with AWS Amplify Auth and sends it in the `Authorization` header. Set `FRONTEND_ORIGIN` when deploying if the frontend is hosted outside the local Vite origin.

## Infrastructure plan

The `CrudAppStack` provisions:

- DynamoDB table for product records
- API Gateway for HTTP access
- Lambda function to host the .NET API
- Cognito user pool for identity and JWT issuance

## Operational checklist

- Configure AWS credentials in GitHub secrets
- Ensure the account has permission to create DynamoDB, Cognito, API Gateway, and Lambda resources
- Publish the backend before synthesizing/deploying the CDK stack so the Lambda asset contains the latest code
- The DynamoDB table is retained when the stack is removed to avoid accidental data loss
- Run the CI workflow before promoting production deployment
- Validate the application with a smoke test after deployment
