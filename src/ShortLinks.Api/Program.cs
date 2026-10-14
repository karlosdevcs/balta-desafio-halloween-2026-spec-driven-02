using Microsoft.EntityFrameworkCore;
using ShortLinks.Api.Application;
using ShortLinks.Api.Data;
using ShortLinks.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Detail ??= context.ProblemDetails.Status switch
    {
        StatusCodes.Status400BadRequest => "A requisição é inválida.",
        StatusCodes.Status404NotFound => "O recurso solicitado não existe.",
        StatusCodes.Status500InternalServerError => "Ocorreu um erro inesperado.",
        _ => null
    });

builder.Services.AddDbContext<ShortLinksDbContext>((services, options) =>
    options.UseSqlite(services.GetRequiredService<IConfiguration>().GetConnectionString("ShortLinks")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICodeGenerator, RandomCodeGenerator>();
builder.Services.AddScoped<ILinkRepository, LinkRepository>();
builder.Services.AddScoped<LinkService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ShortLinksDbContext>().Database.Migrate();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapLinkEndpoints();

app.Run();
