using System.Text;
using Api.Middleware;
using Application.Activities;
using Application.Interfaces;
using AutoMapper;
using Domain;
using FluentValidation.AspNetCore;
using Infrastructure.security;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Persistence;

namespace Api
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    
    public void ConfigureDevelopmentServices(IServiceCollection services){
      services.AddDbContext<DataContext>(opt =>
      {
          opt.UseLazyLoadingProxies();
          opt.UseSqlite(Configuration.GetConnectionString("DefaultConnection"));
      });

         ConfigureServices(services);
        }
    public void ConfigureProductionServices(IServiceCollection services){
      services.AddDbContext<DataContext>(opt =>
      {
          opt.UseLazyLoadingProxies();
          //opt.UseMySql(Configuration.GetConnectionString("DefaultConnection"));
      });

         ConfigureServices(services);
        }
    public void ConfigureServices(IServiceCollection services)
    {


      // We will have a lot of handlers but we need to tell mediator once
      services.AddMediatR(typeof(List.Handler).Assembly);

      services.AddAutoMapper(typeof(List.Handler));

      services.AddControllers( opt =>
      {
          var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
          opt.Filters.Add(new AuthorizeFilter(policy));

      }).AddFluentValidation(
        cfg => {
          cfg.RegisterValidatorsFromAssemblyContaining<Create>();
        }
      );

      var builder = services.AddIdentityCore<AppUser>();
      var identitybuilder = new IdentityBuilder(builder.UserType, builder.Services);
      identitybuilder.AddEntityFrameworkStores<DataContext>();
      identitybuilder.AddSignInManager<SignInManager<AppUser>>();

      services.AddScoped<IJwtGenerator, JwtGenerator>();
      services.AddScoped<IUserAccessor, UserAccessor>();

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokenkey"]));

      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
      {
          opt.TokenValidationParameters = new TokenValidationParameters
          {
              ValidateIssuerSigningKey = true,
              IssuerSigningKey = key,
              ValidateAudience = false,
              ValidateIssuer = false
          };
      });

        }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseMiddleware<ErrorHandlinMiddleware>();
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }
  }
}
