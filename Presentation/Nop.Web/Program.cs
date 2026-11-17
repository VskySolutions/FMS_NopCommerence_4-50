using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Nop.Core.Configuration;
using Nop.Web.Framework.Infrastructure.Extensions;
//using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Configuration.AddJsonFile(NopConfigurationDefaults.AppSettingsFilePath, true, true);
builder.Configuration.AddEnvironmentVariables();

//Add services to the application and configure service provider
builder.Services.ConfigureApplicationServices(builder);

var app = builder.Build();

//Configure the application HTTP request pipeline
app.ConfigureRequestPipeline();
app.StartEngine();

app.Run();

//Rotativa Setup
//IWebHostEnvironment env = app.Environment;
//RotativaConfiguration.Setup((Microsoft.AspNetCore.Hosting.IHostingEnvironment)env);
