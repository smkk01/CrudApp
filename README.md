# CrudOps Cloud

A React and .NET product CRUD app deployed on AWS with Amplify Hosting, API Gateway, Lambda, DynamoDB, and Cognito.

## Live deployment

- **Frontend:** [CrudOps Cloud](https://main.d31r4v0tw7ckk9.amplifyapp.com/)
- **API base URL:** `https://58gn5kkv5i.execute-api.ap-southeast-1.amazonaws.com/prod/`
- **Health check:** [API health](https://58gn5kkv5i.execute-api.ap-southeast-1.amazonaws.com/prod/health)
- **AWS region:** `ap-southeast-1`
- **Amplify app ID:** `d31r4v0tw7ckk9`

The dashboard requires a Cognito account. Choose **Create an account**, verify the email using the code Cognito sends, and then sign in. Product endpoints require a valid Cognito token; `/health` is public. Product records are stored in DynamoDB.

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

The backend and infrastructure are deployed to the AWS account described above. To update the stack manually from PowerShell at the repository root:

```powershell
dotnet publish backend\CrudApi\CrudApi.csproj -c Release -o artifacts\CrudApi
npm install -g aws-cdk
$env:FRONTEND_ORIGIN = "https://main.d31r4v0tw7ckk9.amplifyapp.com"

Set-Location infrastructure\CrudAppCdk
cdk deploy --app "dotnet run --project CrudAppCdk.csproj --configuration Release" --require-approval never
```

The backend must be published to `artifacts/CrudApi` before CDK deployment. The custom Lambda handler uses `Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer` to deserialize API Gateway events.

To deploy from GitHub Actions instead, configure `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` repository secrets, then manually run the **Deploy** workflow. The workflow publishes the backend and sets the deployed Amplify origin for CORS.

## AWS Amplify frontend hosting

The frontend is hosted by Amplify from the `main` branch of [`smkk01/CrudApp`](https://github.com/smkk01/CrudApp). The repository includes an Amplify monorepo build spec in `amplify.yml`; Amplify builds automatically when changes are pushed to `main`. The deployed app uses:

- `VITE_API_BASE_URL` — API Gateway URL above
- `VITE_AWS_REGION` — `ap-southeast-1`
- `VITE_COGNITO_USER_POOL_ID` — `ap-southeast-1_Wb9ZOX4bF`
- `VITE_COGNITO_USER_POOL_CLIENT_ID` — `1steojb3dpo8lh6rri5bgod3o4`

The Amplify SPA rewrite `</^[^.]+$/>` to `/index.html` (status `200`) is configured. It serves extensionless app routes without rewriting static asset requests such as JavaScript, CSS, and SVG files. The CDK stack restricts API CORS to `https://main.d31r4v0tw7ckk9.amplifyapp.com`, including API Gateway authorization-error responses.

## Notes

- The backend uses an in-memory product repository by default for local development.
- The deployed Lambda uses the DynamoDB table provisioned by CDK; `PRODUCT_TABLE` and `DynamoDb__TableName` are configured by the stack.
- The CDK stack provisions a DynamoDB table with a retain-on-stack-removal policy, a Cognito user pool, and API Gateway/Lambda.
- `/health` is public; all product routes require a valid Cognito ID token. CORS is restricted to `FRONTEND_ORIGIN` (defaults to `http://localhost:5173`).
