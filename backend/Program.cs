using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using HydroFlowManager.API.Data;
using HydroFlowManager.API.Models;
using HydroFlowManager.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddAuthorization();
// Serializar/Desserializar enums como strings para compatibilidade com o frontend
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
var app = builder.Build();
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/orders"))
    {
        await next();
        return;
    }

    // se o ModelState tiver erro, logar
    context.Response.OnStarting(() =>
    {
        // Evita depender de tipos internos que podem não existir; em vez disso,
        // verifica o endpoint resolvido pela request via IEndpointFeature.
        var endpoint = context.Features.Get<Microsoft.AspNetCore.Http.Features.IEndpointFeature>()?.Endpoint;
        if (endpoint?.DisplayName?.Contains("HTTP: PUT /orders/{id}") == true)
        {
            // aqui seria avançado; em muitos casos não é necessário
        }
        return Task.CompletedTask;
    });

    await next();
});
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // ✅ Aplica migrations pendentes automaticamente
    db.Database.Migrate();

    if (!db.Attendants.Any())
    {
        var salt = SecurityHelper.GenerateSalt();
        var hash = SecurityHelper.HashPassword("123456", salt);
        db.Attendants.Add(new Attendant { CPF = "00000000000", Name = "admin", PasswordHash = hash, PasswordSalt = salt });
        db.SaveChanges();
    }

    if (!db.Services.Any())
    {
        db.Services.AddRange(new[]
        {
            new Service { Name = "Lavagem Simples", PriceMotorcycle = 15, PriceCarSmall = 25, PriceCarLarge = 35, DurationMinutes = 20, Active = true },
            new Service { Name = "Lavagem Completa", PriceMotorcycle = 25, PriceCarSmall = 40, PriceCarLarge = 55, DurationMinutes = 40, Active = true },
            new Service { Name = "Polimento", PriceMotorcycle = 50, PriceCarSmall = 80, PriceCarLarge = 110, DurationMinutes = 90, Active = true }
        });
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
    Console.WriteLine($"[POST] /auth/login - CPF: {dto.CPF}");
    var att = await db.Attendants.FindAsync(dto.CPF);
    if (att == null)
    {
        Console.WriteLine("❌ Login falhou: CPF não encontrado");
        return Results.Unauthorized();
    }
    if (!SecurityHelper.VerifyPassword(dto.Password, att.PasswordHash, att.PasswordSalt))
    {
        Console.WriteLine("❌ Login falhou: senha incorreta");
        return Results.Unauthorized();
    }
    var token = SecurityHelper.GenerateToken(att, builder.Configuration["Jwt:Key"]);
    Console.WriteLine($"✅ Login bem-sucedido: {att.Name}");
    return Results.Ok(new { token });
});

// Endpoints de leitura públicos para facilitar o consumo pelo frontend sem token
app.MapGet("/clients", async (AppDbContext db) =>
{
    Console.WriteLine("[GET] /clients");
    return await db.Clients.ToListAsync();
});

app.MapPost("/clients", [Authorize] async (Client c, AppDbContext db) =>
{
    Console.WriteLine($"[POST] /clients - Nome: {c.Name}, CPF/CNPJ: {c.CPFCNPJ}");
    if (await db.Clients.AnyAsync(x => x.CPFCNPJ == c.CPFCNPJ))
    {
        Console.WriteLine("❌ Cliente já existe");
        return Results.Conflict("Cliente já existe");
    }
    db.Clients.Add(c);
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Cliente criado com sucesso");
    return Results.Created($"/clients/{c.CPFCNPJ}", c);
});

app.MapPut("/clients/{cpfcnpj}", [Authorize] async (string cpfcnpj, Client updated, AppDbContext db) =>
{
    Console.WriteLine($"[PUT] /clients/{cpfcnpj}");
    var client = await db.Clients.FindAsync(cpfcnpj);
    if (client is null)
    {
        Console.WriteLine("❌ Cliente não encontrado");
        return Results.NotFound("Cliente não encontrado");
    }
    client.Name = updated.Name;
    client.Email = updated.Email;
    client.Phone = updated.Phone;
    client.Observations = updated.Observations;
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Cliente atualizado com sucesso");
    return Results.Ok(client);
});

app.MapDelete("/clients/{cpfcnpj}", [Authorize] async (string cpfcnpj, AppDbContext db) =>
{
    Console.WriteLine($"[DELETE] /clients/{cpfcnpj}");
    var client = await db.Clients.FindAsync(cpfcnpj);
    if (client is null)
    {
        Console.WriteLine("❌ Cliente não encontrado");
        return Results.NotFound("Cliente não encontrado");
    }
    db.Clients.Remove(client);
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Cliente removido com sucesso");
    return Results.NoContent();
});

app.MapGet("/vehicles", async (AppDbContext db) =>
{
    Console.WriteLine("[GET] /vehicles");
    return await db.Vehicles.Include(v => v.Client).ToListAsync();
});

app.MapPost("/vehicles", [Authorize] async (Vehicle v, AppDbContext db) =>
{
    Console.WriteLine($"[POST] /vehicles - Placa: {v.Plate}, Tipo: {v.Type}");
    if (await db.Clients.FindAsync(v.ClientId) is null)
    {
        Console.WriteLine("❌ Cliente não encontrado");
        return Results.BadRequest("Cliente não encontrado");
    }
    db.Vehicles.Add(v);
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Veículo criado com sucesso");
    return Results.Created($"/vehicles/{v.Plate}", v);
});

app.MapPut("/vehicles/{plate}", [Authorize] async (string plate, Vehicle updated, AppDbContext db) =>
{
    Console.WriteLine($"[PUT] /vehicles/{plate}");
    var vehicle = await db.Vehicles.FindAsync(plate);
    if (vehicle is null)
    {
        Console.WriteLine("❌ Veículo não encontrado");
        return Results.NotFound("Veículo não encontrado");
    }
    if (await db.Clients.FindAsync(updated.ClientId) is null)
    {
        Console.WriteLine("❌ Cliente não encontrado");
        return Results.BadRequest("Cliente não encontrado");
    }
    vehicle.Type = updated.Type;
    vehicle.ClientId = updated.ClientId;
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Veículo atualizado com sucesso");
    return Results.Ok(vehicle);
});

app.MapDelete("/vehicles/{plate}", [Authorize] async (string plate, AppDbContext db) =>
{
    Console.WriteLine($"[DELETE] /vehicles/{plate}");
    var vehicle = await db.Vehicles.FindAsync(plate);
    if (vehicle is null)
    {
        Console.WriteLine("❌ Veículo não encontrado");
        return Results.NotFound("Veículo não encontrado");
    }
    db.Vehicles.Remove(vehicle);
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Veículo removido com sucesso");
    return Results.NoContent();
});

app.MapGet("/services", async (AppDbContext db) =>
{
    Console.WriteLine("[GET] /services");
    return await db.Services.ToListAsync();
});

app.MapPost("/services", [Authorize] async (Service s, AppDbContext db) =>
{
    Console.WriteLine($"[POST] /services - Nome: {s.Name}");
    db.Services.Add(s);
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Serviço criado com sucesso");
    return Results.Created($"/services/{s.Id}", s);
});

app.MapPut("/services/{id}", [Authorize] async (int id, Service updated, AppDbContext db) =>
{
    Console.WriteLine($"[PUT] /services/{id}");
    var service = await db.Services.FindAsync(id);
    if (service is null)
    {
        Console.WriteLine("❌ Serviço não encontrado");
        return Results.NotFound("Serviço não encontrado");
    }
    service.Name = updated.Name;
    service.PriceMotorcycle = updated.PriceMotorcycle;
    service.PriceCarSmall = updated.PriceCarSmall;
    service.PriceCarLarge = updated.PriceCarLarge;
    service.DurationMinutes = updated.DurationMinutes;
    service.Active = updated.Active;
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Serviço atualizado com sucesso");
    return Results.Ok(service);
});

app.MapDelete("/services/{id}", [Authorize] async (int id, AppDbContext db) =>
{
    Console.WriteLine($"[DELETE] /services/{id}");
    var service = await db.Services.FindAsync(id);
    if (service is null)
    {
        Console.WriteLine("❌ Serviço não encontrado");
        return Results.NotFound("Serviço não encontrado");
    }
    db.Services.Remove(service);
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Serviço removido com sucesso");
    return Results.NoContent();
});

app.MapGet("/orders", async (AppDbContext db) =>
{
    //Console.WriteLine("[GET] /orders");
    Console.WriteLine("🚀 [GET] /orders foi chamado");
    return await db.Orders.Include(o => o.Vehicle).ThenInclude(v => v.Client).Include(o => o.Items).Include(o => o.Attendant).ToListAsync();
});



app.MapPost("/orders", [Authorize] async (OrderCreateDto dto, AppDbContext db) =>
{
    Console.WriteLine($"[POST] /orders - Veículo: {dto.VehiclePlate}, Atendente: {dto.AttendantCPF}");
    var vehicle = await db.Vehicles.FindAsync(dto.VehiclePlate);
    if (vehicle is null)
    {
        Console.WriteLine("❌ Veículo não encontrado");
        return Results.BadRequest("Veículo não encontrado");
    }
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
        if (svc is null)
        {
            Console.WriteLine($"❌ Serviço {it.ServiceId} não encontrado");
            return Results.BadRequest($"Serviço {it.ServiceId} não encontrado");
        }
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
    Console.WriteLine($"✅ Ordem criada com sucesso: ID={order.Id}, Total={order.Total}");
    return Results.Created($"/orders/{order.Id}", order);
});

app.MapPut("/orders/{id}/status", [Authorize] async (Guid id, OrderStatus status, AppDbContext db) =>
{
    Console.WriteLine($"[PUT] /orders/{id}/status - Novo status: {status}");
    var order = await db.Orders.FindAsync(id);
    if (order is null)
    {
        Console.WriteLine("❌ Ordem não encontrada");
        return Results.NotFound("Ordem não encontrada");
    }

    order.Status = status;
    await db.SaveChangesAsync();
    Console.WriteLine($"✅ Status da ordem atualizado para: {status}");
    return Results.Ok(order);
});

// 🔥 ENDPOINT EDITAR ORDEM DE SERVIÇO - COM LOGS
app.MapPut("/orders/{id}", [Authorize] async (Guid id, OrderUpdateDto dto, AppDbContext db) =>
{
    Console.WriteLine("🔧 [PUT] /orders/{id} - EDITAR ORDEM DE SERVIÇO");
    Console.WriteLine($"   ID recebido: {id}");

    try
    {
        Console.WriteLine("   DTO recebido (JSON): " + JsonSerializer.Serialize(dto));
    }
    catch (Exception ex)
    {
        Console.WriteLine("   (não foi possível serializar o DTO recebido) " + ex.Message);
    }

    var order = await db.Orders
        .Include(o => o.Items)
        .Include(o => o.Vehicle) // se ainda não estiver incluído
        .FirstOrDefaultAsync(o => o.Id == id);

    if (order is null)
    {
        Console.WriteLine("   ❌ Ordem não encontrada no banco.");
        return Results.NotFound("Ordem não encontrada");
    }

    Console.WriteLine($"   Ordem encontrada: Status={order.Status}, Total atual={order.Total}");

    if (order.Status != OrderStatus.Open)
    {
        Console.WriteLine("   ❌ Ordem não está em status Open. Edição não permitida.");
        return Results.BadRequest("Só é possível editar ordens em aberto");
    }

    Console.WriteLine("   🔄 Limpando itens antigos da ordem...");
    order.Items.Clear();
    decimal newTotal = 0;

    if (dto.Items is null || dto.Items.Count == 0)
    {
        Console.WriteLine("   ⚠️ DTO.Items está vazio ou null.");
    }
    else
    {
        Console.WriteLine($"   DTO.Items contém {dto.Items.Count} item(ns).");

        foreach (var it in dto.Items)
        {
            Console.WriteLine($"   -> Processando item: ServiceId={it.ServiceId}, Quantity={it.Quantity}");

            var svc = await db.Services.FindAsync(it.ServiceId);
            if (svc is null)
            {
                Console.WriteLine($"   ❌ Serviço {it.ServiceId} não encontrado no banco!");
                return Results.BadRequest($"Serviço {it.ServiceId} não encontrado");
            }

            // aqui você usa seu método de preço por tipo de veículo
            var unitPrice = svc.GetPriceFor(order.Vehicle.Type);

            var newItem = new OrderItem
            {
                OrderId = order.Id,
                ServiceId = svc.Id,
                Quantity = it.Quantity,
                UnitPrice = unitPrice
            };
            order.Items.Add(newItem);
            newTotal += unitPrice * it.Quantity;
        }
    }

    // converte int -> enum de volta
    order.PaymentMethod = (PaymentMethod)dto.PaymentMethod;

    order.Total = newTotal;

    Console.WriteLine($"   ✅ Ordem editada com sucesso! Novo total = {order.Total}, PaymentMethod = {order.PaymentMethod}");

    await db.SaveChangesAsync();
    return Results.Ok(order);
});

// 🔥 ENDPOINT CONFIRMAR PAGAMENTO - COM LOGS
app.MapPut("/orders/{id}/pay", [Authorize] async (Guid id, OrderPaymentDto dto, AppDbContext db) =>
{
    Console.WriteLine($"💰 [PUT] /orders/{id}/pay - CONFIRMAR PAGAMENTO");
    Console.WriteLine($"   Método de pagamento: {dto.PaymentMethod}");

    var order = await db.Orders.FindAsync(id);
    if (order is null)
    {
        Console.WriteLine("❌ Ordem não encontrada");
        return Results.NotFound("Ordem não encontrada");
    }

    Console.WriteLine($"   Ordem encontrada: Status={order.Status}, Total={order.Total}");

    if (order.Status != OrderStatus.Open)
    {
        Console.WriteLine("❌ Só é possível confirmar pagamento de ordens em aberto");
        return Results.BadRequest("Só é possível confirmar pagamento de ordens em aberto");
    }

    order.PaymentMethod = dto.PaymentMethod;
    order.Status = OrderStatus.Paid;

    await db.SaveChangesAsync();
    Console.WriteLine($"✅ Pagamento confirmado! Status alterado para: {order.Status}");
    return Results.Ok(order);
});

app.MapDelete("/orders/{id}", [Authorize] async (Guid id, AppDbContext db) =>
{
    Console.WriteLine($"[DELETE] /orders/{id}");
    var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
    if (order is null)
    {
        Console.WriteLine("❌ Ordem não encontrada");
        return Results.NotFound("Ordem não encontrada");
    }
    db.Orders.Remove(order);
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Ordem removida com sucesso");
    return Results.NoContent();
});

app.MapGet("/cash/summary", [Authorize] async (DateTime? date, AppDbContext db) =>
{
    Console.WriteLine($"[GET] /cash/summary - Data: {date?.ToString("yyyy-MM-dd") ?? "hoje"}");
    var target = date?.Date ?? DateTime.UtcNow.Date;
    var orders = await db.Orders.Include(o => o.Items).Where(o => o.CreatedAt.Date == target).ToListAsync();
    var totalOrders = orders.Count;
    var totalReceita = orders.Sum(x => x.Total);
    var totalDescontos = orders.Sum(x => x.Discount);
    var byPayment = orders.GroupBy(x => x.PaymentMethod).Select(g => new { Payment = g.Key.ToString(), Total = g.Sum(x => x.Total) }).ToList();
    var byService = orders.SelectMany(o => o.Items).GroupBy(i => i.ServiceId).Select(g => new { ServiceId = g.Key, Quantity = g.Sum(i => i.Quantity), Total = g.Sum(i => i.UnitPrice * i.Quantity) }).ToList();
    Console.WriteLine($"✅ Resumo: {totalOrders} ordens, Receita total: {totalReceita}");
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