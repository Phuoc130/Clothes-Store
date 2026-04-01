using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductStore.Models;
using ProductStore.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ProductDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ProductDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "CHANGE_ME_TO_A_LONG_SECURE_KEY_1234567890_ABCDEF";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ProductStore";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ProductStore.Client";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Read token from Authorization header first, then cookies
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    System.Console.WriteLine($"[JWT] Reading token from Authorization header");
                    context.Token = token;
                }
                else if (context.Request.Cookies.TryGetValue("auth_token", out var cookieToken))
                {
                    System.Console.WriteLine($"[JWT] Reading token from cookie");
                    context.Token = cookieToken;
                }
                return System.Threading.Tasks.Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IVnPayService, VnPayService>();
var backendAssembly = typeof(ProductDbContext).Assembly;
var orderEmailServiceInterface = backendAssembly.GetType("ProductStore.Services.IOrderEmailService");
var orderEmailServiceImplementation = backendAssembly.GetType("ProductStore.Services.OrderEmailService");
if (orderEmailServiceInterface != null && orderEmailServiceImplementation != null)
{
    builder.Services.AddScoped(orderEmailServiceInterface, orderEmailServiceImplementation);
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ProductDbContext>();
    db.Database.EnsureCreated();
    EnsureShopOrderCheckoutColumns(db);
    SeedData.Initialize(db);
    await IdentitySeedData.InitializeAsync(services);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseCors("ClientPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void EnsureShopOrderCheckoutColumns(ProductDbContext db)
{
    db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[ShopOrders]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('ShopOrders', 'SubTotalAmount') IS NULL
        ALTER TABLE [ShopOrders] ADD [SubTotalAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_ShopOrders_SubTotalAmount] DEFAULT(0);

    IF COL_LENGTH('ShopOrders', 'DiscountAmount') IS NULL
        ALTER TABLE [ShopOrders] ADD [DiscountAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_ShopOrders_DiscountAmount] DEFAULT(0);

    IF COL_LENGTH('ShopOrders', 'ShippingFee') IS NULL
        ALTER TABLE [ShopOrders] ADD [ShippingFee] decimal(18,2) NOT NULL CONSTRAINT [DF_ShopOrders_ShippingFee] DEFAULT(0);

    IF COL_LENGTH('ShopOrders', 'FullName') IS NULL
        ALTER TABLE [ShopOrders] ADD [FullName] nvarchar(128) NOT NULL CONSTRAINT [DF_ShopOrders_FullName] DEFAULT('');

    IF COL_LENGTH('ShopOrders', 'PhoneNumber') IS NULL
        ALTER TABLE [ShopOrders] ADD [PhoneNumber] nvarchar(20) NOT NULL CONSTRAINT [DF_ShopOrders_PhoneNumber] DEFAULT('');

    IF COL_LENGTH('ShopOrders', 'ProvinceCity') IS NULL
        ALTER TABLE [ShopOrders] ADD [ProvinceCity] nvarchar(128) NOT NULL CONSTRAINT [DF_ShopOrders_ProvinceCity] DEFAULT('');

    IF COL_LENGTH('ShopOrders', 'District') IS NULL
        ALTER TABLE [ShopOrders] ADD [District] nvarchar(128) NOT NULL CONSTRAINT [DF_ShopOrders_District] DEFAULT('');

    IF COL_LENGTH('ShopOrders', 'AddressLine') IS NULL
        ALTER TABLE [ShopOrders] ADD [AddressLine] nvarchar(300) NOT NULL CONSTRAINT [DF_ShopOrders_AddressLine] DEFAULT('');

    IF COL_LENGTH('ShopOrders', 'OrderNote') IS NULL
        ALTER TABLE [ShopOrders] ADD [OrderNote] nvarchar(500) NULL;

    IF COL_LENGTH('ShopOrders', 'PaymentMethod') IS NULL
        ALTER TABLE [ShopOrders] ADD [PaymentMethod] nvarchar(20) NOT NULL CONSTRAINT [DF_ShopOrders_PaymentMethod] DEFAULT('cod');

    IF COL_LENGTH('ShopOrders', 'CouponCode') IS NULL
        ALTER TABLE [ShopOrders] ADD [CouponCode] nvarchar(30) NULL;
END
");
}

