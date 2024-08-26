using Authentication.Helpers;
using Aythentication.Context;
using Aythentication.Models;
using Azure.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Aythentication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _authContext;
        public UserController(AppDbContext appDbContext)
        {
            _authContext = appDbContext;

        }
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObj)

        {
   

            if (userObj == null)
                return BadRequest();

            var user =await _authContext.Users.FirstOrDefaultAsync(x =>x.Username == userObj.Username && x.Password == userObj.Password);

            if (user == null)
                return NotFound (new
                {
                    Message ="Username And Password Is In Correct"
                });

            user.Token =CreateJwt(user);

            return Ok(
                new
                {
                    Token = user.Token,
                    Message ="Login Success!"
                });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Registration([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest("User data is null");


            if (await CheckUserNameExistAsync(userObj.Username))
                return BadRequest(new
                {
                    Message = "Username Already Exist!"
                });

            if (await CheckEmailExistAsync(userObj.Email))
                return BadRequest(new
                {
                    Message = "Email Already Exist!"
                });
            var pass = CheckPasswordStrength(userObj.Password);
            if (!string.IsNullOrEmpty(pass))
                return BadRequest(new
                {
                    Message = pass.ToString()
                });


            userObj.Password =PasswordHasher.HashPassword(userObj.Password);
            userObj.Role = "User";
            userObj.Token = "";
            // Ensure Id is not set
            if (userObj.Id != 0)
                return BadRequest("Id should not be set for new registrations");

            await _authContext.Users.AddAsync(userObj);
            await _authContext.SaveChangesAsync();

            return Ok(new { Message = "User Registered!" });
        }

        private  Task <bool> CheckUserNameExistAsync(string userName)
            =>_authContext.Users.AnyAsync(x=> x.Username ==userName );

        private Task<bool> CheckEmailExistAsync(string email)
     => _authContext.Users.AnyAsync(x => x.Email == email);


        private string CheckPasswordStrength(string password)
        {
            StringBuilder sb = new StringBuilder();

            // Check minimum length
            if (password.Length < 8)
                sb.Append("Minimum password length should be 8" + Environment.NewLine);

            // Check if password is alphanumeric (has lower, upper case letters and digits)
            if (!(Regex.IsMatch(password, "[a-z]") && Regex.IsMatch(password, "[A-Z]") && Regex.IsMatch(password, "[0-9]")))
                sb.Append("Password should be alphanumeric" + Environment.NewLine);

            // Check if password contains special characters
            if (!Regex.IsMatch(password, @"[<>@!#$%^&*()\-+[\]{}?:;|\\,./~`]"))
                sb.Append("Password should contain special characters" + Environment.NewLine);

            return sb.ToString();
        }

        private string CreateJwt(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("verysecretkey12345678901234567890"); // 32 characters
            var identity = new ClaimsIdentity(new Claim[]
            {
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = credentials
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }


    }
}
