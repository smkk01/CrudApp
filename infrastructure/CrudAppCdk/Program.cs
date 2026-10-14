using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Lambda;
using Constructs;

var app = new App();

new CrudAppStack(app, "CrudAppStack", new StackProps
{
    Env = new Amazon.CDK.Environment
    {
        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
        Region = System.Environment.GetEnvironmentVariable("AWS_REGION")
            ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
            ?? "ap-southeast-1"
    }
});

app.Synth();

public static class AppPaths
{
    public static string GetPublishedApiPath()
    {
        var currentDir = System.Environment.CurrentDirectory;
        var candidate = currentDir;

        while (!string.IsNullOrEmpty(candidate))
        {
            if (Directory.Exists(Path.Combine(candidate, "backend", "CrudApi"))
                && Directory.Exists(Path.Combine(candidate, "infrastructure", "CrudAppCdk")))
            {
                var publishedPath = Path.Combine(candidate, "artifacts", "CrudApi");
                if (!File.Exists(Path.Combine(publishedPath, "CrudApi.dll")))
                {
                    throw new InvalidOperationException(
                        $"Published API assembly not found at '{publishedPath}'. Run dotnet publish before deploying the CDK stack.");
                }

                return publishedPath;
            }

            var parent = Directory.GetParent(candidate);
            if (parent is null)
            {
                break;
            }

            candidate = parent.FullName;
        }

        throw new InvalidOperationException("Could not locate the CrudApp repository root while resolving the published API asset.");
    }
}

public sealed class CrudAppStack : Stack
{
    public CrudAppStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        var productTable = new Table(this, "ProductsTable", new TableProps
        {
            TableName = "crudapp-products",
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute { Name = "id", Type = AttributeType.STRING },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            RemovalPolicy = RemovalPolicy.RETAIN,
            Stream = StreamViewType.NEW_IMAGE
        });

        productTable.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "CategoryIndex",
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute { Name = "category", Type = AttributeType.STRING },
            ProjectionType = ProjectionType.ALL
        });

        productTable.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "UpdatedAtIndex",
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute { Name = "updatedAt", Type = AttributeType.STRING },
            ProjectionType = ProjectionType.ALL
        });

        var userPool = new UserPool(this, "CrudAppUsers", new UserPoolProps
        {
            UserPoolName = "crudapp-users",
            SelfSignUpEnabled = true,
            SignInAliases = new SignInAliases { Email = true },
            AutoVerify = new AutoVerifiedAttrs { Email = true },
            StandardAttributes = new StandardAttributes
            {
                Email = new StandardAttribute { Required = true }
            }
        });

        var userPoolClient = userPool.AddClient("WebClient", new UserPoolClientProps
        {
            AuthFlows = new AuthFlow { UserPassword = true, UserSrp = true }
        });
        var authorizer = new CognitoUserPoolsAuthorizer(this, "CrudApiAuthorizer", new CognitoUserPoolsAuthorizerProps
        {
            CognitoUserPools = new[] { userPool }
        });
        var frontendOrigin = System.Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "http://localhost:5173";

        var lambdaFunction = new Function(this, "CrudApiLambda", new FunctionProps
        {
            Runtime = Runtime.DOTNET_8,
            Handler = "CrudApi::CrudApi.ApiFunction::FunctionHandler",
            Code = Code.FromAsset(AppPaths.GetPublishedApiPath()),
            MemorySize = 512,
            Timeout = Duration.Seconds(30),
            Environment = new Dictionary<string, string>
            {
                ["PRODUCT_TABLE"] = productTable.TableName,
                ["COGNITO_USERPOOL_ID"] = userPool.UserPoolId,
                ["COGNITO_WEB_CLIENT_ID"] = userPoolClient.UserPoolClientId,
                ["DynamoDb__Enabled"] = "true",
                ["DynamoDb__TableName"] = productTable.TableName,
                ["ALLOWED_ORIGIN"] = frontendOrigin,
                ["APP_REGION"] = this.Region
            }
        });

        productTable.GrantReadWriteData(lambdaFunction);

        var api = new RestApi(this, "CrudApiGateway", new RestApiProps
        {
            RestApiName = "crudapp-api",
            DefaultCorsPreflightOptions = new CorsOptions
            {
                AllowOrigins = new[] { frontendOrigin },
                AllowMethods = Cors.ALL_METHODS,
                AllowHeaders = new[] { "Authorization", "Content-Type" }
            },
            DeployOptions = new StageOptions
            {
                LoggingLevel = MethodLoggingLevel.INFO,
                DataTraceEnabled = true,
                MetricsEnabled = true
            }
        });
        var gatewayCorsHeaders = new Dictionary<string, string>
        {
            ["Access-Control-Allow-Origin"] = $"'{frontendOrigin}'",
            ["Access-Control-Allow-Headers"] = "'Authorization,Content-Type'",
            ["Access-Control-Allow-Methods"] = "'GET,POST,PUT,DELETE,OPTIONS'",
            ["Vary"] = "'Origin'"
        };
        foreach (var (responseId, responseType) in new[]
        {
            ("Unauthorized", ResponseType_.UNAUTHORIZED),
            ("AccessDenied", ResponseType_.ACCESS_DENIED),
            ("Default4xx", ResponseType_.DEFAULT_4XX),
            ("Default5xx", ResponseType_.DEFAULT_5XX)
        })
        {
            api.AddGatewayResponse($"Cors{responseId}", new GatewayResponseOptions
            {
                Type = responseType,
                ResponseHeaders = gatewayCorsHeaders
            });
        }

        var integration = new LambdaIntegration(lambdaFunction);
        api.Root.AddResource("health").AddMethod("GET", integration);

        var protectedMethodOptions = new MethodOptions
        {
            Authorizer = authorizer,
            AuthorizationType = AuthorizationType.COGNITO
        };
        var productsResource = api.Root.AddResource("api").AddResource("products");
        productsResource.AddMethod("GET", integration, protectedMethodOptions);
        productsResource.AddMethod("POST", integration, protectedMethodOptions);

        var productResource = productsResource.AddResource("{id}");
        productResource.AddMethod("GET", integration, protectedMethodOptions);
        productResource.AddMethod("PUT", integration, protectedMethodOptions);
        productResource.AddMethod("DELETE", integration, protectedMethodOptions);

        new CfnOutput(this, "ApiGatewayUrl", new CfnOutputProps
        {
            Value = api.Url,
            Description = "Base URL for the CrudApp API Gateway"
        });

        new CfnOutput(this, "UserPoolId", new CfnOutputProps
        {
            Value = userPool.UserPoolId,
            Description = "Cognito user pool ID"
        });

        new CfnOutput(this, "UserPoolClientId", new CfnOutputProps
        {
            Value = userPoolClient.UserPoolClientId,
            Description = "Cognito user pool app client ID for the frontend"
        });
    }
}
