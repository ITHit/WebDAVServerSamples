using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CalDAVServer.FileSystemStorage.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.webdav.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();
builder.Services.AddWebDav(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });
app.UseHttpsRedirection();
// Basic auth requires SSL connection. To enable non - SSL connection for testing purposes read the following articles:
// - In case of Windows & MS Office: http://support.microsoft.com/kb/2123563
// - In case of Mac OS X & MS Office: https://support.microsoft.com/en-us/kb/2498069
app.UseBasicAuth();
app.UseProvisioninge();
app.UseWebDav(app.Environment);

app.Run();
