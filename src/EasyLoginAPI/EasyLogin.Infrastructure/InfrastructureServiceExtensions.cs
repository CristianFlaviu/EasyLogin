using EasyLogin.Application.Interfaces;
using EasyLogin.Infrastructure.Identity;
using EasyLogin.Infrastructure.Identity.MappingProfiles;
using EasyLogin.Infrastructure.Persistence;
using EasyLogin.Infrastructure.Services;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EasyLogin.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddIdentity<AppIdentityUser, AppIdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var jwtKey = config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt__Key is not configured.");
        var jwtIssuer = config["Jwt:Issuer"] ?? "EasyLogin";
        var jwtAudience = config["Jwt:Audience"] ?? "EasyLogin";

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        var emailProvider = config["Email:Provider"]
            ?? throw new InvalidOperationException("Email__Provider is not configured.");

        switch (emailProvider)
        {
            case "SendGrid":
                services.AddScoped<IEmailService, SendGridEmailService>();
                break;
            case "Smtp":
                services.AddScoped<IEmailService, SmtpEmailService>();
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown Email__Provider value '{emailProvider}'. Valid values: SendGrid, Smtp.");
        }

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddHttpContextAccessor();

        var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
        typeAdapterConfig.Scan(typeof(IdentityMappingConfig).Assembly);
        services.AddSingleton(typeAdapterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
