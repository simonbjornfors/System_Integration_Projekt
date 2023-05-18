using AuthApi;
using AuthApi.Model;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<UserContext>(opt => opt.UseInMemoryDatabase("UserAccounts"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/register", async (User user, UserContext db) =>
{
    user.SetPassword(user.Password);
    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();

    return Results.Created("/login", "Registered user successfully!");

});
app.MapGet("/user/{id}", async (int id, UserContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(user);
});

app.MapPost("/login", async (User userLogin, UserContext db) =>
{

    User? user = await db.Users.FirstOrDefaultAsync(user => user.Email.Equals(userLogin.Email));

    if (user == null || !VerifyPassword(user.Password, userLogin.Password))
    {
        return Results.NotFound("The username or password is not correct!");
    }

    var secretkey = builder.Configuration["Jwt:Key"];

    if (secretkey == null)
    {
        return Results.StatusCode(500);
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.GivenName, user.Name),
        new Claim(ClaimTypes.Surname, user.Name),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var token = new JwtSecurityToken
    (
        issuer: builder.Configuration["Jwt:Issuer"],
        audience: builder.Configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(30),
        notBefore: DateTime.UtcNow,
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretkey)), SecurityAlgorithms.HmacSha256)
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(tokenString);


});

static bool VerifyPassword(string hashedPasswordWithSalt, string passwordToVerify)
{
    var parts = hashedPasswordWithSalt.Split('.');
    var salt = Convert.FromBase64String(parts[0]);
    var password = parts[1];

    string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
        password: passwordToVerify,
        salt: salt,
        prf: KeyDerivationPrf.HMACSHA256,
        iterationCount: 10000,
        numBytesRequested: 256 / 8));

    return password == hashed;
}

app.Run();
