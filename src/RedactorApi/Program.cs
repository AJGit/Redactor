using System.Diagnostics;
using RedactorApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args);
//  var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
//builder.AddPresidio("presidio");

//builder.Services.AddLogging();
builder.Services.AddProblemDetails(setup =>
{
    setup.CustomizeProblemDetails = context =>
    {
        if (!context.ProblemDetails.Extensions.ContainsKey("traceId"))
        {
            var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            context.ProblemDetails.Extensions.Add(new KeyValuePair<string, object?>("traceId", traceId));
        }
        context.ProblemDetails.Extensions.Remove("exception");
        if (context.ProblemDetails.Status == 500)
        {
            context.ProblemDetails.Detail = "An error occured in our API. Use the trace id when contacting us.";
        }
    };
});
// builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.SetupKestrel();
builder.Services.MapIocServices();

builder.Services.AddOpenApi();

// Add CORS policy for Chrome extension
// commenting for now
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy(
//         "ExtensionPolicy",
//         policy =>
//         {
//             policy
//                 .WithOrigins(
//                     "chrome-extension://*", // Allow Chrome extension
//                     "http://localhost:5287", // Allow your test website
//                     "https://localhost:5287" // Allow HTTPS if needed
//                 )
//                 .AllowAnyMethod()
//                 .AllowAnyHeader()
//                 .WithExposedHeaders("Content-Disposition"); // If you need to expose any headers
//         }
//     );
// });
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy(
//         "AllowAllOrigins",
//         builder =>
//         {
//             builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
//         }
//     );
// });

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowChatGPT",
        policy =>
        {
            policy
                .WithOrigins(
                    "https://chat.openai.com",
                    "https://chatgpt.com",
                    "http://localhost:5287"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed((_) => true); // Be careful with this in production
        });
});

builder.Services.Configure<ScalarOptions>(options => options.Title = "Redactor API");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.EnabledTargets = [ScalarTarget.CSharp, ScalarTarget.JavaScript, ScalarTarget.PowerShell, ScalarTarget.Shell];
        options
            .WithCdnUrl("https://cdn.jsdelivr.net/npm/@scalar/api-reference")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithFavicon("/favicon.png")
            //.WithPreferredScheme(AuthConstants.ApiKey)
            // .WithApiKeyAuthentication(x => x.Token = "my-api-key")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithTitle("Redactor API reference -- {documentName}");
    });
}
app.UseStaticFiles();

app.UseCors("AllowChatGPT");

app.UseStatusCodePages /*UseExceptionHandler*/(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async httpContext =>
    {
        var pds = httpContext.RequestServices.GetService<IProblemDetailsService>();
        if (pds == null || !await pds.TryWriteAsync(new ProblemDetailsContext { HttpContext = httpContext }))
        {
            await httpContext.Response.WriteAsync("An error occurred.");
        }
    });
});

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async httpContext =>
    {
        var pds = httpContext.RequestServices.GetService<IProblemDetailsService>();
        if (pds == null
            || !await pds.TryWriteAsync(new()
            {
                HttpContext = httpContext
            }))
        {
            // Fallback behavior
            await httpContext.Response.WriteAsync("An internal error occurred.");
        }
    });
});

app.MapApplicationEndpoints();
app.MapDefaultEndpoints();

app.Run();
