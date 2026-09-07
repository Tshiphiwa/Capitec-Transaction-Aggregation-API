using System.Reflection;
using System.Text;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Services;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace Capitec_Transaction_Aggregation_API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppPersistence(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var isTestEnvironment = environment.IsEnvironment("Testing");
        var defaultConnection = configuration.GetConnectionString("DefaultConnection");

        if (!isTestEnvironment && string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");
        }

        if (isTestEnvironment)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    defaultConnection,
                    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null)));
        }

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");

        return services;
    }

    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var isTestEnvironment = environment.IsEnvironment("Testing");
        var jwtKey = configuration["Jwt:Key"] ?? (isTestEnvironment ? "ThisIsATestJwtKeyForIntegrationTesting123!" : throw new InvalidOperationException("JWT key is not configured."));
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "CapitecTransactionAPI";
        var jwtAudience = configuration["Jwt:Audience"] ?? "CapitecTransactionAPIClient";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITransactionSourceClient, TransactionSourceClient>();
        services.AddScoped<ICategorizationService, CategorizationService>();
        services.AddScoped<ITransactionReferenceChecker, TransactionReferenceChecker>();
        services.AddScoped<ITransactionMapper, TransactionMapper>();
        services.AddScoped<ITransactionIngestionProcessor, TransactionIngestionProcessor>();
        services.AddScoped<ICardTransactionsService, CardTransactionsService>();
        services.AddScoped<IEftTransactionsService, EftTransactionsService>();
        services.AddScoped<IWalletTransactionsService, WalletTransactionsService>();
        services.AddScoped<IIngestionService, IngestionService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserRoleAccessor, UserRoleAccessor>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Capitec Transaction Aggregation API",
                Version = "v1",
                Description = "API for aggregating and managing financial transactions."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your bearer token in the format: Bearer {token}"
            });

            options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer"),
                        new List<string>()
                    }
                });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowedOrigins", policy =>
            {
                var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["https://localhost:4200", "http://localhost:4200"];

                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
