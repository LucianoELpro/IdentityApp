using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<JWTService>();

//be able to inject JWTService Class inside our Controllers
builder.Services.AddDbContext<Context>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

//be able  to auhtenticate  users  using JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            //validate the  token  based  on the key we have provided inside appsetting.development.json  JWT:Key
            ValidateIssuerSigningKey = true,
            // the issuer signing key base on JWT:Key
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:key"])),
            // the issuer  which  in here the api project  url we are using
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            //validate  the issuer (who ever is issuing the JWT)
            ValidateIssuer = true,
            //don�t validate audience(angular side)
            ValidateAudience = false
        };
    });






//defining our IdentityCore Service
builder.Services.AddIdentityCore<User>(options =>
{
    //Password configuration
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    //for email confirmation
    options.Password.RequiredLength = 6;
})
    .AddRoles<IdentityRole>() //be able to add roles
    .AddRoleManager<RoleManager<IdentityRole>>() // be able  to make use of RoleManager
    .AddEntityFrameworkStores<Context>()//providing our context
    .AddSignInManager<SignInManager<User>>()//make use of  SignIn Manager
    .AddUserManager<UserManager<User>>()//Make user  of UserManager  to create users
    .AddDefaultTokenProviders();//ba able to create tokens for email confirmation

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// adding  UseAuthentication into our pipeline and this should come before UseAutorization
// Authentication  verifies the identity of a user or service and authorization determines their access rigths
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
