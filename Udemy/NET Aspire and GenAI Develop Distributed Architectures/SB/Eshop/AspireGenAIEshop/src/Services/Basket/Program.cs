
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache(connectionName: "cache");
builder.Services.AddScoped<BasketService>();
builder.Services.AddMassTransitWithAssemblies(Assembly.GetExecutingAssembly());
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<CatalogApiClient>(client =>
{
    client.BaseAddress = new("https+http://catalog");
});

builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: "Subhasish",
        configureOptions: options =>
        {
            options.RequireHttpsMetadata = false;
            options.Audience = "account";
        });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapBasketEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.Run();
