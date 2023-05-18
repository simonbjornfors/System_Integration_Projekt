using Microsoft.EntityFrameworkCore;
using LibraryApi;
using LibraryApi.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<LibraryContext>(opt => opt.UseInMemoryDatabase("Library"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/books", [Authorize] async (LibraryContext db) =>
{
    var books = await db.Books.ToListAsync();
    return Results.Ok(books);
});
app.MapGet("/book/{id}", [Authorize] async (int id, LibraryContext db) =>
{
    var book = await db.Books.FindAsync(id);
    if (book == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(book);
});
app.MapPost("/book", [Authorize] async (Book book, LibraryContext db) =>
{
    await db.Books.AddAsync(book);
    await db.SaveChangesAsync();
    return Results.Created($"/book/{book.Id}", book);
});
app.MapPost("/loan", [Authorize] async (Loan loan, LibraryContext db) =>
{
    var book = await db.Books.FindAsync(loan.BookId);
    if (book == null || book.IsBorrowed)
    {
        return Results.NotFound("Book is not available");
    }
    book.IsBorrowed = true;
    await db.Loans.AddAsync(loan);
    await db.SaveChangesAsync();
    return Results.Created($"/loan/{loan.Id}", loan);
});
app.MapPost("/return", [Authorize] async (Loan loan, LibraryContext db) =>
{
    var existingLoan = await db.Loans.FindAsync(loan.Id);
    if (existingLoan == null)
    {
        return Results.NotFound("Loan not found");
    }

    var book = await db.Books.FindAsync(existingLoan.BookId);
    if (book == null)
    {
        return Results.NotFound("Book not found");
    }

    book.IsBorrowed = false;
    db.Loans.Remove(existingLoan);
    await db.SaveChangesAsync();
    return Results.Ok("Book returned");
});
app.Run();

