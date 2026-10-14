# CrudOps Cloud

A production-ready AWS React CRUD starter using React 19, Vite, Material UI, .NET 8 Minimal API, DynamoDB, API Gateway, Cognito, and AWS CDK.

## Project structure

- `frontend/` — React + TypeScript + MUI dashboard
- `backend/CrudApi/` — .NET 8 Minimal API with CRUD routes and validation
- `backend/CrudApi.Tests/` — xUnit tests for the product repository
- `infrastructure/CrudAppCdk/` — AWS CDK .NET app for DynamoDB, Lambda, API Gateway, and Cognito
- `.github/workflows/` — CI/CD automation
- `docs/` — additional deployment notes

## Local development

### Frontend

```bash
cd frontend
npm install
npm run dev
```

The UI expects the backend at `http://localhost:5155` by default. If you run the API on another port, set:

```bash
VITE_API_BASE_URL=http://localhost:5155
```

For AWS sign-in, copy `frontend/.env.example` to `frontend/.env.local` and set the API URL, Cognito user-pool ID, app-client ID, and AWS region from the CDK stack outputs. The browser uses Cognito to create and verify accounts and sign in; product API calls include the current Cognito ID token.

### Backend

```bash
cd backend/CrudApi
dotnet run
```

Swagger UI is available in development mode at `https://localhost:5001/swagger` or the port shown by the CLI.

## Testing

```bash
dotnet test CrudApp.sln --configuration Release
npm run build --prefix frontend
```

## AWS deployment

1. Publish the API:

```bash
dotnet publish backend/CrudApi/CrudApi.csproj -c Release -o artifacts/CrudApi
```

2. Install CDK dependencies:

```bash
npm install -g aws-cdk
```

3. Deploy the infrastructure:

```bash
cd infrastructure/CrudAppCdk
cdk deploy --app "dotnet run --project CrudAppCdk.csproj --configuration Release" --require-approval never
```

When deploying the custom Lambda entry point for API Gateway, publish the backend first to `artifacts/CrudApi` and ensure the Lambda assembly includes the `Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer` attribute. This is required for `APIGatewayProxyRequest` deserialization on the AWS runtime.

Alternatively, use GitHub Actions to automate the workflow.

## AWS Amplify frontend hosting

The repository includes an Amplify monorepo build spec in `amplify.yml` for the Vite frontend. In the Amplify Console, connect the GitHub repository `smkk01/CrudApp` and the `main` branch. Set these build environment variables from the CDK outputs:

- `VITE_API_BASE_URL` — the `ApiGatewayUrl` output
- `VITE_AWS_REGION` — the stack region
- `VITE_COGNITO_USER_POOL_ID` — the `UserPoolId` output
- `VITE_COGNITO_USER_POOL_CLIENT_ID` — the `UserPoolClientId` output

After Amplify assigns the branch domain, set `FRONTEND_ORIGIN` to `https://main.<app-id>.amplifyapp.com` and redeploy the CDK stack so API CORS matches the hosted frontend. Add an Amplify rewrite rule from `/<*>` to `/index.html` with status `200` for client-side routing.

## Notes

- The backend uses an in-memory product repository by default for local development.
- The deployed Lambda uses the DynamoDB table provisioned by CDK; `PRODUCT_TABLE` and `DynamoDb__TableName` are configured by the stack.
- The CDK stack provisions a DynamoDB table with a retain-on-stack-removal policy, a Cognito user pool, and API Gateway/Lambda.
- `/health` is public; all product routes require a valid Cognito ID token. CORS is restricted to `FRONTEND_ORIGIN` (defaults to `http://localhost:5173`).
