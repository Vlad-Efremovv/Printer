var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSwagger();
app.UseSwaggerUI();

// ”казываем базовый адрес
var baseAddress = "https://localhost:8080";

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseHsts();

app.Run(baseAddress);
