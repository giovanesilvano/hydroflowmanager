using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HydroFlowManager.API.Data;
using HydroFlowManager.API.Models;
using HydroFlowManager.API.DTOs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "HydroFlow Manager API", Version = "v1" });

    // Configuração para autenticação JWT no Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Insira o token JWT no campo abaixo usando o formato: Bearer {seu_token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Configuration["Jwt:Key"] = builder.Configuration["Jwt:Key"] ?? "DevSecretReplaceBeforeProduction";
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=hydroflow.db"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (!db.Attendants.Any())
    {
        var salt = SecurityHelper.GenerateSalt();
        var hash = SecurityHelper.HashPassword("123456", salt);
        db.Attendants.Add(new Attendant { CPF = "00000000000", Name = "admin", PasswordHash = hash, PasswordSalt = salt });
        db.SaveChanges();
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapPost("/auth/login", async (LoginDto dto, AppDbContext db) =>
{
    var att = await db.Attendants.FindAsync(dto.CPF);
    if (att == null) return Results.Unauthorized();
    if (!SecurityHelper.VerifyPassword(dto.Password, att.PasswordHash, att.PasswordSalt)) return Results.Unauthorized();
    var token = SecurityHelper.GenerateToken(att, builder.Configuration["Jwt:Key"]);
    return Results.Ok(new { token });
});
app.MapGet("/clients", [Microsoft.AspNetCore.Authorization.Authorize] async (AppDbContext db) => await db.Clients.ToListAsync());
app.MapPost("/clients", [Microsoft.AspNetCore.Authorization.Authorize] async (Client c, AppDbContext db) =>
{
    if (await db.Clients.AnyAsync(x => x.CPFCNPJ == c.CPFCNPJ)) return Results.Conflict("Cliente já existe");
    db.Clients.Add(c);
    await db.SaveChangesAsync();
    return Results.Created($"/clients/{c.CPFCNPJ}", c);
});
app.MapPut("/clients/{cpfcnpj}", [Microsoft.AspNetCore.Authorization.Authorize] async (string cpfcnpj, Client updated, AppDbContext db) =>
{
    var client = await db.Clients.FindAsync(cpfcnpj);
    if (client is null) return Results.NotFound("Cliente não encontrado");
    client.Name = updated.Name;
    client.Email = updated.Email;
    client.Phone = updated.Phone;
    client.Observations = updated.Observations;
    await db.SaveChangesAsync();
    return Results.Ok(client);
});
app.MapDelete("/clients/{cpfcnpj}", [Microsoft.AspNetCore.Authorization.Authorize] async (string cpfcnpj, AppDbContext db) =>
{
    var client = await db.Clients.FindAsync(cpfcnpj);
    if (client is null) return Results.NotFound("Cliente não encontrado");
    db.Clients.Remove(client);
    await db.SaveChangesAsync();
    return Results.NoContent();
});
app.MapGet("/vehicles", [Microsoft.AspNetCore.Authorization.Authorize] async (AppDbContext db) => await db.Vehicles.Include(v => v.Client).ToListAsync());
app.MapPost("/vehicles", [Microsoft.AspNetCore.Authorization.Authorize] async (Vehicle v, AppDbContext db) =>
{
    if (await db.Clients.FindAsync(v.ClientId) is null) return Results.BadRequest("Cliente não encontrado");
    db.Vehicles.Add(v);
    await db.SaveChangesAsync();
    return Results.Created($"/vehicles/{v.Plate}", v);
});
app.MapPut("/vehicles/{plate}", [Microsoft.AspNetCore.Authorization.Authorize] async (string plate, Vehicle updated, AppDbContext db) =>
{
    var vehicle = await db.Vehicles.FindAsync(plate);
    if (vehicle is null) return Results.NotFound("Veículo não encontrado");
    if (await db.Clients.FindAsync(updated.ClientId) is null) return Results.BadRequest("Cliente não encontrado");
    vehicle.Type = updated.Type;
    vehicle.ClientId = updated.ClientId;
    await db.SaveChangesAsync();
    return Results.Ok(vehicle);
});
app.MapDelete("/vehicles/{plate}", [Microsoft.AspNetCore.Authorization.Authorize] async (string plate, AppDbContext db) =>
{
    var vehicle = await db.Vehicles.FindAsync(plate);
    if (vehicle is null) return Results.NotFound("Veículo não encontrado");
    db.Vehicles.Remove(vehicle);
    await db.SaveChangesAsync();
    return Results.NoContent();
});
app.MapGet("/services", [Microsoft.AspNetCore.Authorization.Authorize] async (AppDbContext db) => await db.Services.ToListAsync());
app.MapPost("/services", [Microsoft.AspNetCore.Authorization.Authorize] async (Service s, AppDbContext db) =>
{
    db.Services.Add(s);
    await db.SaveChangesAsync();
    return Results.Created($"/services/{s.Id}", s);
});
app.MapPut("/services/{id}", [Microsoft.AspNetCore.Authorization.Authorize] async (int id, Service updated, AppDbContext db) =>
{
    var service = await db.Services.FindAsync(id);
    if (service is null) return Results.NotFound("Serviço não encontrado");
    service.Name = updated.Name;
    service.PriceMotorcycle = updated.PriceMotorcycle;
    service.PriceCarSmall = updated.PriceCarSmall;
    service.PriceCarLarge = updated.PriceCarLarge;
    service.DurationMinutes = updated.DurationMinutes;
    service.Active = updated.Active;
    await db.SaveChangesAsync();
    return Results.Ok(service);
});
app.MapDelete("/services/{id}", [Microsoft.AspNetCore.Authorization.Authorize] async (int id, AppDbContext db) =>
{
    var service = await db.Services.FindAsync(id);
    if (service is null) return Results.NotFound("Serviço não encontrado");
    db.Services.Remove(service);
    await db.SaveChangesAsync();
    return Results.NoContent();
});
app.MapGet("/orders", [Microsoft.AspNetCore.Authorization.Authorize] async (AppDbContext db) =>
    await db.Orders.Include(o => o.Vehicle).ThenInclude(v => v.Client).Include(o => o.Items).Include(o => o.Attendant).ToListAsync());
app.MapGet("/orders/{id}", [Microsoft.AspNetCore.Authorization.Authorize] async (Guid id, AppDbContext db) =>
{
    var order = await db.Orders.Include(o => o.Vehicle).ThenInclude(v => v.Client).Include(o => o.Items).Include(o => o.Attendant).FirstOrDefaultAsync(o => o.Id == id);
    if (order is null) return Results.NotFound("Ordem não encontrada");
    return Results.Ok(order);
});
app.MapPost("/orders", [Microsoft.AspNetCore.Authorization.Authorize] async (OrderCreateDto dto, AppDbContext db) =>
{
    var vehicle = await db.Vehicles.FindAsync(dto.VehiclePlate);
    if (vehicle is null) return Results.BadRequest("Veículo não encontrado");
    var order = new Order
    {
        Id = Guid.NewGuid(),
        VehiclePlate = vehicle.Plate,
        AttendantCPF = dto.AttendantCPF,
        CreatedAt = DateTime.UtcNow,
        Status = OrderStatus.Open,
    };
    decimal subtotal = 0M;
    foreach (var it in dto.Items)
    {
        var svc = await db.Services.FindAsync(it.ServiceId);
        if (svc is null) return Results.BadRequest($"Serviço {it.ServiceId} não encontrado");
        var price = svc.GetPriceFor(vehicle.Type);
        var oi = new OrderItem { ServiceId = svc.Id, Quantity = it.Quantity, UnitPrice = price };
        order.Items.Add(oi);
        subtotal += price * it.Quantity;
    }
    order.Subtotal = subtotal;
    order.Discount = dto.PaymentMethod == PaymentMethod.Cash ? Math.Round(subtotal * 0.10M, 2) : 0M;
    order.Total = subtotal - order.Discount;
    order.PaymentMethod = dto.PaymentMethod;
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/orders/{order.Id}", order);
});
app.MapPut("/orders/{id}/status", [Microsoft.AspNetCore.Authorization.Authorize] async (Guid id, OrderStatus status, AppDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound("Ordem não encontrada");
    order.Status = status;
    await db.SaveChangesAsync();
    return Results.Ok(order);
});
app.MapDelete("/orders/{id}", [Microsoft.AspNetCore.Authorization.Authorize] async (Guid id, AppDbContext db) =>
{
    var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
    if (order is null) return Results.NotFound("Ordem não encontrada");
    db.Orders.Remove(order);
    await db.SaveChangesAsync();
    return Results.NoContent();
});
app.MapGet("/cash/summary", [Microsoft.AspNetCore.Authorization.Authorize] async (DateTime? date, AppDbContext db) =>
{
    var target = date?.Date ?? DateTime.UtcNow.Date;
    var orders = await db.Orders.Include(o => o.Items).Where(o => o.CreatedAt.Date == target).ToListAsync();
    var totalOrders = orders.Count;
    var totalReceita = orders.Sum(x => x.Total);
    var totalDescontos = orders.Sum(x => x.Discount);
    var byPayment = orders.GroupBy(x => x.PaymentMethod).Select(g => new { Payment = g.Key.ToString(), Total = g.Sum(x => x.Total) }).ToList();
    var byService = orders.SelectMany(o => o.Items).GroupBy(i => i.ServiceId).Select(g => new { ServiceId = g.Key, Quantity = g.Sum(i => i.Quantity), Total = g.Sum(i => i.UnitPrice * i.Quantity) }).ToList();
    return Results.Ok(new { Date = target, TotalOrders = totalOrders, TotalReceita = totalReceita, TotalDescontos = totalDescontos, ByPayment = byPayment, ByService = byService });
});

app.Run();

static class SecurityHelper
{
    public static byte[] GenerateSalt()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);
        return salt;
    }
    public static byte[] HashPassword(string password, byte[] salt)
    {
        using var h = new System.Security.Cryptography.HMACSHA256(salt);
        return h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
    }
    public static bool VerifyPassword(string password, byte[] hash, byte[] salt)
    {
        var candidate = HashPassword(password, salt);
        return candidate.SequenceEqual(hash);
    }
    public static string GenerateToken(Attendant a, string key)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.CreateToken(new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[] {
                new System.Security.Claims.Claim("cpf", a.CPF),
                new System.Security.Claims.Claim("name", a.Name)
            }),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        });
        return handler.WriteToken(token);
    }
}
