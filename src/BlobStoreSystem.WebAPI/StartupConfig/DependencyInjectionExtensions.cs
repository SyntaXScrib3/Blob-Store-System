using BlobStoreSystem.Domain.Services;
using BlobStoreSystem.Infrastructure.Data;
using BlobStoreSystem.Infrastructure.FileSystem;
using BlobStoreSystem.WebApi.Services;
using BlobStoreSystem.WebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BlobStoreSystem.WebAPI.StartupConfig;

public static class DependencyInjectionExtensions
{
    public static void AddStandardServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }

    public static void AddAuthServices(this WebApplicationBuilder builder)
    {
        var secretKey = builder.Configuration["Authentication:SecretKey"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(opts =>
        {
            opts.RequireHttpsMetadata = false;
            opts.SaveToken = true;
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Authentication:Issuer"],
                ValidAudience = builder.Configuration["Authentication:Audience"],
                IssuerSigningKey = key
            };
        });

        //builder.Services.AddAuthorization(options =>
        //{
        //    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        //        .RequireAuthenticatedUser()
        //        .Build();
        //});
    }

    public static void AddDbContextServices(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("BlobStoreDb");

        builder.Services.AddDbContext<BlobStoreDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("BlobStoreSystem.WebAPI");
            });
        });
    }

    public static void AddCustomServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });


        builder.Services.AddHttpContextAccessor();

        builder.Services.AddSingleton<IBlobStorageProvider>(_ =>
        {
            var localPath = Path.Combine("D:\\Projects\\GitLabProjects\\blob-store-system", "BlobStorage");
            return new LocalFileSystemBlobStorage(localPath);
        });
        builder.Services.AddScoped<IFsProvider, EfFsProvider>(sp =>
        {
            var dbContext = sp.GetRequiredService<BlobStoreDbContext>();
            var blobStorage = sp.GetRequiredService<IBlobStorageProvider>();
            var currentUser = sp.GetRequiredService<ICurrentUserService>();

            var userId = currentUser.IsAuthenticated ? currentUser.UserId : Guid.Empty;

            //var userId = Guid.Empty;

            return new EfFsProvider(dbContext, blobStorage, userId);
        });
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddHostedService<OrphanBlobCleanupService>();
    }
}
