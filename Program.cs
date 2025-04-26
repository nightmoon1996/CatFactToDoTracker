using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TodoList.Data;
using TodoList.Models;
using TodoList.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc; // Needed for Problem details
using Microsoft.Extensions.Logging; // Add this using statement
using System.IdentityModel.Tokens.Jwt; // Add this for JwtRegisteredClaimNames

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
// ... other settings ...
var keyString = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not found in AddJwtBearer.");
var keyBytes = Encoding.UTF8.GetBytes(keyString); // Correct encoding
var securityKey = new SymmetricSecurityKey(keyBytes); // Correct key type
var catFactApiUrl = builder.Configuration["CatFactApiUrl"] ?? "https://catfact.ninja/";
var weatherApiUrl = builder.Configuration["WeatherApiUrl"] ?? "https://api.open-meteo.com/"; // Add this line

// --- Services ---

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("TodoListDb"));

// Add custom services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TodoService>();

// Add HttpClientFactory
builder.Services.AddHttpClient("CatFactClient", client =>
{
    client.BaseAddress = new Uri(catFactApiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Add HttpClient for Weather API
builder.Services.AddHttpClient("WeatherClient", client =>
{
    client.BaseAddress = new Uri(weatherApiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Resolve configuration *inside* the options setup using builder.Configuration
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not found in AddJwtBearer.");
    var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not found in AddJwtBearer.");
    var keyString = jwtSettings["Key"]?.Trim() ?? throw new InvalidOperationException("JWT Key not found in AddJwtBearer or is empty after trimming.");
    var keyBytes = Encoding.UTF8.GetBytes(keyString);
    var securityKey = new SymmetricSecurityKey(keyBytes);

    options.MapInboundClaims = false; // <-- Add this line to prevent default claim mapping

    options.RequireHttpsMetadata = builder.Environment.IsProduction();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = securityKey,
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = JwtRegisteredClaimNames.Sub // Explicitly map Identity.Name from 'sub' claim
    };

    // Restore default events (or remove if no custom logic needed beyond logging)
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // Basic logging is often sufficient unless deep debugging
            Console.WriteLine($"JWT Authentication Failed: {context.Exception?.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"JWT Token Validated for user: {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        }
        // Remove OnChallenge and OnMessageReceived unless specific customization is needed
    };
});

// Add Authorization
builder.Services.AddAuthorization();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Cat Fact Todo API", Version = "v1" });

    // Configure Swagger to use JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// --- Middleware Pipeline ---

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cat Fact Todo API V1");
        // Optional: Serve Swagger UI at the app's root
        // c.RoutePrefix = string.Empty;
    });
    app.UseDeveloperExceptionPage(); // More detailed errors in dev
}
else
{
    app.UseExceptionHandler("/error"); // Basic error handling for production
    app.UseHsts(); // Add HSTS headers
}

app.UseHttpsRedirection(); // Redirect HTTP to HTTPS

app.UseAuthentication(); // Enable authentication middleware
app.UseAuthorization();  // Enable authorization middleware

// --- Minimal API Endpoints ---

// Error endpoint for production
app.MapGet("/error", () => Results.Problem("An unexpected error occurred.")).ExcludeFromDescription();

// Auth Endpoints
app.MapPost("/api/auth/register", async (RegisterModel model, AuthService authService) =>
{
    // Pass the whole model to the service
    var user = await authService.RegisterAsync(model);
    // Check if user creation was successful (user is not null)
    return user != null ? Results.Ok(new { message = "User registered successfully." }) : Results.BadRequest("Username already exists.");
})
.AllowAnonymous()
.Produces(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest) // Use ProblemDetails for consistency
.WithName("RegisterUser")
.WithTags("Authentication");

app.MapPost("/api/auth/login", async (LoginModel model, AuthService authService) =>
{
    // Pass the whole model to the service
    var token = await authService.LoginAsync(model);
    // Check if login was successful (token is not null)
    return !string.IsNullOrEmpty(token) ? Results.Ok(new { token }) : Results.Unauthorized();
})
.AllowAnonymous()
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.WithName("LoginUser")
.WithTags("Authentication");

// Todo Endpoints (Require Authentication)
app.MapGet("/api/todos", async (ClaimsPrincipal user, TodoService todoService) =>
{
    var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
    // Validate and parse the user ID
    if (userIdString == null || !int.TryParse(userIdString, out var userId))
    {
        return Results.Unauthorized();
    }

    // Call service with the int userId
    var todos = await todoService.GetTodosByUserIdAsync(userId);
    return Results.Ok(todos);
})
.RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme }) // Explicit Scheme
.Produces<List<TodoViewModel>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.WithName("GetTodos")
.WithTags("Todos");

app.MapPost("/api/todos", async (TodoCreateModel model, ClaimsPrincipal user, TodoService todoService) =>
{
    // --- Add Detailed Claim Logging ---
    Console.WriteLine("--- Claims in /api/todos POST ---");
    if (user?.Identity?.IsAuthenticated ?? false)
    {
        foreach (var claim in user.Claims)
        {
            Console.WriteLine($"  Claim Type: {claim.Type}, Value: {claim.Value}");
        }
    }
    else
    {
        Console.WriteLine("  User is NOT authenticated.");
    }
    Console.WriteLine("---------------------------------");
    // --- End Logging ---

    var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
    Console.WriteLine($"Value found for NameIdentifier: '{userIdString ?? "NULL"}'"); // Log the found value

    // Validate and parse the user ID
    if (userIdString == null || !int.TryParse(userIdString, out var userId))
    {
        Console.WriteLine($"Unauthorized: NameIdentifier ('{userIdString ?? "NULL"}') missing or not an integer."); // Log reason for 401
        return Results.Unauthorized();
    }

    // Pass the model and int userId to the service
    var createdTodoItem = await todoService.CreateTodoAsync(model, userId);
    if (createdTodoItem == null)
    {
        return Results.Problem("Failed to create Todo item.");
    }

    // Fetch weather description for the newly created item
    string weatherDesc = await todoService.GetWeatherDescriptionAsync(createdTodoItem.Date);

    // Map TodoItem to TodoViewModel for the response
    var createdTodoViewModel = new TodoViewModel
    {
        Id = createdTodoItem.Id,
        Message = createdTodoItem.Message,
        Date = createdTodoItem.Date,
        CatFact = createdTodoItem.CatFact,
        WeatherDescription = weatherDesc // Populate weather description
    };

    return Results.Created($"/api/todos/{createdTodoViewModel.Id}", createdTodoViewModel);
})
.RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme }) // Explicit Scheme
.Produces<TodoViewModel>(StatusCodes.Status201Created)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
.WithName("CreateTodo")
.WithTags("Todos");


// Test Authentication Endpoint
app.MapGet("/api/testauth", (ClaimsPrincipal user) =>
{
    // Log details from the ClaimsPrincipal if reached
    Console.WriteLine($"--- [/api/testauth Handler Reached] ---");
    if (user?.Identity?.IsAuthenticated ?? false) // Add null check for safety
    {
        Console.WriteLine($"User Authenticated: {user.Identity.IsAuthenticated}");
        Console.WriteLine($"Authentication Type: {user.Identity.AuthenticationType}");
        Console.WriteLine($"User Name (Identity.Name): {user.Identity.Name}"); // Now mapped from 'sub'
        Console.WriteLine($"--- All Claims in /api/testauth ---");
        foreach (var claim in user.Claims)
        {
            Console.WriteLine($"  Claim Type: {claim.Type}, Value: {claim.Value}");
        }
        Console.WriteLine("-----------------------------------");
    }
    else
    {
        Console.WriteLine("User is NOT authenticated in /api/testauth.");
    }
    return Results.Ok($"Hello authenticated user: {user?.Identity?.Name ?? "Unknown"}!");
})
.RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme }) // Explicit Scheme
.WithName("TestAuth")
.WithTags("Test");

app.Run();

// Make Program public for WebApplicationFactory - MUST BE AT THE END
public partial class Program { }
