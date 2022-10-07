using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<ItemsContext>(opt =>
    opt.UseInMemoryDatabase("ItemList"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using (var scope = app.Services.CreateScope())
    using (var dbContext = scope.ServiceProvider.GetRequiredService<ItemsContext>())
    {
        try
        {
            // NOTE Using EnsureCreated is not recommended for relational db if one plans to use EF Migrations
            dbContext.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            // TODO Log error here
            throw;
        }
    }
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
