using GraphQL;
using GraphQL.Types;
using GraphQL.Utilities.Federation;
using ApolloFederation.SubgraphCompatibility.Products;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(builder =>
        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod())
);

// Add GraphQL builder.Services and configure options
builder.Services
    .AddSingleton<Query>()
    .AddSingleton<AnyScalarGraphType>()
    .AddSingleton<ServiceGraphType>()
    .AddSingleton<ISchema>(provider =>
    {
        return FederatedSchema.For(File.ReadAllText("products.graphql"), schemaBuilder =>
        {
            schemaBuilder.ServiceProvider = provider;
            schemaBuilder.Types.Include<Query>();
            // reference resolvers
            schemaBuilder.Types.For(nameof(Product))
                .ResolveReferenceAsync<Product?>(ctx =>
                {
                    var productId = ctx.Arguments.GetValueOrDefault("id")?.ToString();
                    if (!string.IsNullOrEmpty(productId))
                    {
                        return Task.FromResult<Product?>(Data.Products.FirstOrDefault(p => p.Id == productId));
                    }

                    return Task.FromResult<Product?>(null);
                });
            schemaBuilder.Types.For(nameof(DeprecatedProduct))
                .ResolveReferenceAsync<DeprecatedProduct?>(ctx =>
                {
                    var sku = ctx.Arguments["sku"]?.ToString();
                    var pkg = ctx.Arguments["package"]?.ToString();
                    if (!string.IsNullOrEmpty(sku) && !string.IsNullOrEmpty(pkg))
                    {
                        return Task.FromResult(Data.DeprecatedProducts.FirstOrDefault(p => p.Sku == sku && p.Package == pkg));
                    }

                    return Task.FromResult<DeprecatedProduct?>(null);
                });
            schemaBuilder.Types.For(nameof(User))
                .ResolveReferenceAsync<User?>(ctx =>
                {
                    var email = ctx.Arguments["email"]?.ToString();
                    if (!string.IsNullOrEmpty(email))
                    {
                        var user = Data.Users.FirstOrDefault(u => u.Email == email);
                        if (user != null)
                        {
                            string? totalProductsCreated = ctx.Arguments.GetValueOrDefault("totalProductsCreated")?.ToString();
                            if (totalProductsCreated != null)
                            {
                                user = user with { TotalProductsCreated = int.Parse(totalProductsCreated) };
                            }

                            string? yearsOfEmployment = ctx.Arguments.GetValueOrDefault("yearsOfEmployment")?.ToString();
                            if (yearsOfEmployment != null)
                            {
                                user.LengthOfEmployment = int.Parse(yearsOfEmployment);
                            }
                        }
                        return Task.FromResult(user);
                    }

                    return Task.FromResult<User?>(null);
                });
            schemaBuilder.Types.For(nameof(ProductResearch))
                .ResolveReferenceAsync<ProductResearch?>(ctx =>
                {
                    var study = ctx.Arguments.GetValueOrDefault("study");
                    if (study is Dictionary<string, object>)
                    {
                        var caseNumber = ((Dictionary<string, object>)study).GetValueOrDefault("caseNumber")?.ToString();
                        return Task.FromResult(Data.ProductResearches.FirstOrDefault(r => r.Study.CaseNumber == caseNumber));
                    }

                    return Task.FromResult<ProductResearch?>(null);
                });
        });
    })
    .AddGraphQL(graphqlBuilder =>
    {

        graphqlBuilder.UseApolloTracing();
        // graphqlBuilder.ConfigureExecutionOptions(action =>
        // {
        //     action.UnhandledExceptionDelegate =

        // });
        graphqlBuilder.AddSystemTextJson();
        graphqlBuilder.AddErrorInfoProvider(action =>
        {
            action.ExposeExceptionDetails = true;
        });
    });

var app = builder.Build();

app.UseCors();
app.UseRouting();
app.UseGraphQL("/");

app.Run();
// app.Run("http://localhost:4001");
