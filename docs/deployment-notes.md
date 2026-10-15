# Deployment notes

## Frontend hosting

AWS Amplify Hosting is configured through the repository's `amplify.yml` for the Vite frontend. Connect the GitHub `main` branch in Amplify Console, set the four `VITE_*` variables documented in the README, and configure an SPA rewrite with source `</^[^.]+$/>`, target `/index.html`, and status `200`. This matches extensionless client-side routes while leaving static asset paths (which have file extensions) untouched; a catch-all `/<*>` rule rewrites JavaScript and CSS requests to HTML and prevents the app from loading.

The `main` branch domain is `https://main.d31r4v0tw7ckk9.amplifyapp.com`. `FRONTEND_ORIGIN` is set to that origin by the deployment workflow; for manual deployment, set the same variable before running CDK. API Gateway's authorization and gateway error responses also include CORS headers so the browser can handle 401/403 errors.

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
