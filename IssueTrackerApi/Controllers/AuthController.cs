using Microsoft.AspNetCore.Mvc;
using IssueTrackerApi.Data;
using IssueTrackerApi.DTOs;
using IssueTrackerApi.Models;
using IssueTrackerApi.Services;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace IssueTrackerApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var exists = await _context.Users.AnyAsync(x => x.Email == dto.Email);
            if (exists) return BadRequest("User already exists");

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User created");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            var token = _tokenService.CreateToken(user.Email, user.Role);

            return Ok(token);
        }
    }
}