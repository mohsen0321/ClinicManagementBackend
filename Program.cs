using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using clinicsystembackend.Data;
using clinicsystembackend.Services;

var builder = WebApplication.CreateBuilder(args);

// 1.  ”ÃÌ· DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2.  ”ÃÌ· Œœ„… «·‹ JWT
builder.Services.AddScoped<JwtService>();

// 3. ≈⁄œ«œ «·‹ JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ClockSkew = TimeSpan.Zero // ≈·€«¡ √Ì  √ŒÌ— “„‰Ì ≈÷«›Ì ·«‰ Â«¡ «· Êﬂ‰
    };
});

// 4. ≈⁄œ«œ «·‹ CORS ·ﬂÌ Ì „ﬂ‰ React ›Ì «·ŒÿÊ«  «··«Õﬁ… „‰ ≈—”«· Ê«” ﬁ»«· «·»Ì«‰« 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // —«»ÿ  ‘€Ì·  ÿ»Ìﬁ React «·«› —«÷Ì
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<EmailService>();
// 5.  Œ’Ì’ Swagger ·œ⁄„ ≈œŒ«· «·‹ Bearer Token ·· Ã—»…
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Clinic Management API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "√œŒ· «·‹ Token ﬂ«· «·Ì: Bearer YOUR_TOKEN",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//  ›⁄Ì· „Ã·œ «·„·›«  «·À«» … wwwroot · Œ“Ì‰ Ê⁄—÷ «·’Ê—
app.UseStaticFiles();

//  ›⁄Ì· ”Ì«”… «·‹ CORS
app.UseCors("AllowReactApp");

//  — Ì» „Â„ Ãœ«: «·‹ Authentication √Ê·« À„ Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();