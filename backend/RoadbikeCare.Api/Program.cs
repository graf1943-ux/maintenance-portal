using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Roadbike Care API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Entra ID access token"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddScoped<RoadbikeCarePersistenceService>();

var staffAuthority = builder.Configuration["Authentication:Staff:Authority"];
var staffIssuer = builder.Configuration["Authentication:Staff:Issuer"];
var staffAudience = builder.Configuration["Authentication:Staff:Audience"];
var customerAuthority = builder.Configuration["Authentication:Customer:Authority"];
var customerIssuer = builder.Configuration["Authentication:Customer:Issuer"];
var customerAudience = builder.Configuration["Authentication:Customer:Audience"];
if (!builder.Environment.IsDevelopment() &&
    new[] { staffAuthority, staffIssuer, staffAudience, customerAuthority, customerIssuer, customerAudience }
        .Any(string.IsNullOrWhiteSpace))
{
    throw new InvalidOperationException("Both Authentication:Staff and Authentication:Customer authority, issuer, and audience settings are required outside Development.");
}

const string staffScheme = "StaffBearer";
const string customerScheme = "CustomerBearer";
const string portalScheme = "EntraBearer";
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = portalScheme;
        options.DefaultChallengeScheme = portalScheme;
    })
    .AddPolicyScheme(portalScheme, "Entra ID", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return customerScheme;
            }

            try
            {
                var token = authorization["Bearer ".Length..].Trim();
                var issuer = new JwtSecurityTokenHandler().ReadJwtToken(token).Issuer;
                return string.Equals(issuer, staffIssuer, StringComparison.Ordinal) ? staffScheme : customerScheme;
            }
            catch
            {
                return customerScheme;
            }
        };
    })
    .AddJwtBearer(staffScheme, options => ConfigureBearer(options, staffAuthority, staffIssuer, staffAudience))
    .AddJwtBearer(customerScheme, options => ConfigureBearer(options, customerAuthority, customerIssuer, customerAudience, isCustomer: true));

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("PortalUser", policy => policy.RequireRole("Staff", "StaffAdmin", "Customer"));
    options.AddPolicy("Staff", policy => policy.RequireRole("Staff", "StaffAdmin"));
    options.AddPolicy("StaffAdmin", policy => policy.RequireRole("StaffAdmin"));
});

var connectionString = builder.Configuration.GetConnectionString("RoadbikeCare");
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:RoadbikeCare is required outside Development.");
}

builder.Services.AddDbContext<RoadbikeCareDbContext>(options =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseInMemoryDatabase("RoadbikeCare-Development");
    }
    else
    {
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    }
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    }
}));

WebApplication app = builder.Build();
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Roadbike Care API v1"));
}
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<RoadbikeCareDbContext>());
}

app.MapControllers();
app.Run();

static void ConfigureBearer(JwtBearerOptions options, string? authority, string? issuer, string? audience, bool isCustomer = false)
{
    options.Authority = authority;
    options.Audience = audience;
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        NameClaimType = "name",
        RoleClaimType = "roles"
    };
    if (isCustomer)
    {
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity &&
                    !identity.HasClaim("roles", "Customer"))
                {
                    identity.AddClaim(new System.Security.Claims.Claim("roles", "Customer"));
                }

                return Task.CompletedTask;
            }
        };
    }
}

public partial class Program { }
