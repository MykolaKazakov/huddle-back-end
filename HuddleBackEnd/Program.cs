using Microsoft.EntityFrameworkCore;
using HuddleBackEnd;

var builder = WebApplication.CreateBuilder(args);

// 1. Додаємо контролери
builder.Services.AddControllers();

builder.Services.AddDbContext<HuddleDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Додаємо Swagger (для тестування API)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Додаємо CORS, щоб React міг звертатися до API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5175") // React запускається тут
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

// 4. Swagger – доступний лише в режимі розробки
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 5. Використовуємо CORS перед Authorization
app.UseCors("AllowReactApp");

app.UseAuthorization();

// 6. Маршрути до контролерів
app.MapControllers();

app.Run();
