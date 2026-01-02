using System.Text;
using System.Text.Json;
using BookingService.Api.Configuration;
using BookingService.Api.Filters;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using BookingService.Api.Middleware;
using BookingService.Api.Services.Interfaces;
using BookingService.Api.Services.Grpc;
using BookingService.Api.HealthChecks;
using BookingService.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ModelValidationFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
});

// Configure API behavior to suppress automatic model validation response
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

if (builder.Environment.IsDevelopment())
{
    // Add open api and swagger for development
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Booking Service API",
            Version = "v1"
        });

        // Add JWT Authentication to Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header. Enter your JWT token below.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        
        c.OperationFilter<AuthorizeOperationFilter>();

        // Include XML Comments in Swagger
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
        c.EnableAnnotations();
    });
}

builder.Configuration.AddEnvironmentVariables();

// Configure Kafka settings
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));

// Configure JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
var jwtKey = !string.IsNullOrEmpty(jwtSettings.Key) ? jwtSettings.Key : EnvironmentVariables.GetRequiredVariable("JWT_SECRET_KEY");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ProviderOnly", policy =>
        policy.RequireClaim("role", "Provider"))
    .AddPolicy("TenantResource", policy =>
        policy.RequireAuthenticatedUser());

// Configure CORS settings
builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(CorsSettings.SectionName));

// Configure CORS
var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsSettings.AllowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Database configuration
builder.Services.AddBookingDatabase();

// Health checks configuration
builder.Services.AddHealthChecks()
    .AddCheck("self", () =>
    {
        try
        {
            return HealthCheckResult.Healthy("Service is running");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Service check failed", ex);
        }
    }, tags: ["self"])
    .AddNpgSql(
        connectionString: EnvironmentVariables.GetRequiredVariable("DATABASE_CONNECTION_STRING"),
        healthQuery: "SELECT 1;",
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "postgresql"])
    .AddCheck<KafkaHealthCheck>("kafka", tags: ["kafka", "messaging", "db"])
    .AddCheck<AvailabilityGrpcHealthCheck>("availability-grpc", tags: ["grpc", "db"]);

// Register middleware
builder.Services.AddTransient<GlobalExceptionHandler>();
builder.Services.AddTransient<RequestResponseLoggingMiddleware>();

// Register filters
builder.Services.AddScoped<ModelValidationFilter>();

// Register services
builder.Services.AddHttpContextAccessor();

// Configure gRPC client settings
builder.Services.Configure<AvailabilityServiceGrpcSettings>(builder.Configuration.GetSection("AvailabilityServiceGrpc"));

// Register gRPC client
builder.Services.AddScoped<IAvailabilityGrpcClient, AvailabilityGrpcClient>();

// Register application services
builder.Services.AddScoped<IUserContextService, BookingService.Api.Services.UserContextService>();
builder.Services.AddScoped<IBookingService, BookingService.Api.Services.BookingService>();

// Register tenant event service
builder.Services.AddScoped<ITenantEventService, BookingService.Api.Services.TenantEventService>();

// Register service catalog event service
builder.Services.AddScoped<IServiceCatalogEventService, BookingService.Api.Services.ServiceCatalogEventService>();

// Register Kafka producer service
builder.Services.AddSingleton<IKafkaProducerService, BookingService.Api.Services.KafkaProducerService>();

// Register Kafka consumer as background service
builder.Services.AddHostedService<BookingService.Api.Services.KafkaConsumerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookingService API v1");
        c.RoutePrefix = "swagger";
        // Hide the black topbar
        c.HeadContent =
            """
               <style>
                   .swagger-ui .topbar {
                       display: none;
                   }
               </style>
            """;
    });
}

// Add request/response logging middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionHandler>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("AllowFrontend");

// Add /metrics endpoints for Prometheus
app.UseHttpMetrics(); 
app.MapMetrics();

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    AllowCachingResponses = false
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("self"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    AllowCachingResponses = false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    AllowCachingResponses = false
});

app.Run();