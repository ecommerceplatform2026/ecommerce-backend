using Application.Configurations;
using Application.DependencyInjection;
using Infrastructure.Data;
using Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Presentation.Common.Filters;
using Presentation.Common.Middlewares;
using Presentation.Common.Responses;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid request." : error.ErrorMessage)
            .ToList();

        var response = new ApiResponse<object>
        {
            Success = false,
            Data = null,
            Errors = errors.Count > 0 ? errors : new List<string> { "Invalid request." }
        };

        return new BadRequestObjectResult(response);
    };
});

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("JwtSettings"))
    .Validate(settings =>
        !string.IsNullOrWhiteSpace(settings.Secret) &&
        settings.Secret.Length >= 32 &&
        !string.IsNullOrWhiteSpace(settings.Issuer) &&
        !string.IsNullOrWhiteSpace(settings.Audience) &&
        settings.ExpirationHours > 0,
        "JwtSettings must include Secret with at least 32 characters, Issuer, Audience, and ExpirationHours > 0.")
    .ValidateOnStart();

builder.Services
    .AddOptions<CloudinarySettings>()
    .Bind(builder.Configuration.GetSection("CloudinarySettings"))
    .Validate(settings =>
        !string.IsNullOrWhiteSpace(settings.CloudName) &&
        !string.IsNullOrWhiteSpace(settings.ApiKey) &&
        !string.IsNullOrWhiteSpace(settings.ApiSecret),
        "CloudinarySettings must include CloudName, ApiKey, and ApiSecret.")
    .ValidateOnStart();

builder.Services
    .AddOptions<VnPaySettings>()
    .Bind(builder.Configuration.GetSection("VnPay"))
    .Validate(settings =>
        !string.IsNullOrWhiteSpace(settings.TmnCode) &&
        !string.IsNullOrWhiteSpace(settings.HashSecret) &&
        !string.IsNullOrWhiteSpace(settings.PaymentUrl) &&
        !string.IsNullOrWhiteSpace(settings.ReturnUrl),
        "VnPaySettings must include TmnCode, HashSecret, PaymentUrl, and ReturnUrl.")
    .ValidateOnStart();

builder.Services
    .AddOptions<MailSettings>()
    .Bind(builder.Configuration.GetSection("MailSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(option =>
    {
        option.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<EnumSchemaFilter>();
    options.CustomSchemaIds(type => type.FullName);

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme.ToLower(),
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token dạng: Bearer {token}"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<EcommerceContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var allowedOrigins = builder.Configuration
    .GetSection("CorsSettings:AllowedOrigins")
    .Get<string[]>() ?? [];

allowedOrigins = allowedOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Where(origin =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
