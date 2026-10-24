using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartDocs.Api.Data;
using SmartDocs.Api.Dtos;
using SmartDocs.Api.Models;
using SmartDocs.Api.Services;

namespace SmartDocs.Api.Endpoints;

public static class AuthEndpoints
{
    private static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);
    // Hash factice : égalise le temps de réponse quand l'e-mail n'existe pas
    private static readonly string DummyHash = PasswordService.Hash("dummy-password-for-timing");

    public static void MapAuth(this WebApplication app)
    {
        var g = app.MapGroup("/api/auth").RequireRateLimiting("auth");
        g.MapPost("/register", Register);
        g.MapPost("/login", Login);
        g.MapGet("/me", Me).RequireAuthorization();
    }

    private static IResult Bad(string message) => Results.BadRequest(new { error = message });

    private static async Task<IResult> Register(RegisterRequest req, AppDbContext db, TokenService tokens)
    {
        var name = (req.FullName ?? "").Trim();
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var password = req.Password ?? "";

        if (name.Length < 2 || name.Length > 80) return Bad("Le nom complet doit contenir entre 2 et 80 caractères.");
        if (email.Length > 254 || !EmailRx.IsMatch(email)) return Bad("Adresse e-mail invalide.");
        if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsDigit))
            return Bad("Le mot de passe doit contenir au moins 8 caractères, une majuscule et un chiffre.");

        if (await db.Users.AnyAsync(u => u.Email == email))
            return Results.Conflict(new { error = "Un compte existe déjà avec cette adresse e-mail." });

        var user = new User { FullName = name, Email = email, PasswordHash = PasswordService.Hash(password) };
        db.Users.Add(user);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { error = "Un compte existe déjà avec cette adresse e-mail." });
        }
        return Results.Created("/api/auth/me", new AuthResponse(tokens.Create(user), user.ToDto()));
    }

    private static async Task<IResult> Login(LoginRequest req, AppDbContext db, TokenService tokens)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var password = req.Password ?? "";
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        var ok = PasswordService.Verify(password, user?.PasswordHash ?? DummyHash);
        if (user == null || !ok)
            return Results.Json(new { error = "E-mail ou mot de passe incorrect." }, statusCode: StatusCodes.Status401Unauthorized);

        return Results.Ok(new AuthResponse(tokens.Create(user), user.ToDto()));
    }

    private static async Task<IResult> Me(ClaimsPrincipal principal, AppDbContext db)
    {
        var user = await db.Users.FindAsync(principal.UserId());
        return user == null ? Results.Unauthorized() : Results.Ok(user.ToDto());
    }
}
