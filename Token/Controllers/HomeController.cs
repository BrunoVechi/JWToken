using API.Interfaces;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly TokenService _tokenService;
        public HomeController(IUserRepository userRepository, TokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("GetToken")]
        public async Task<ActionResult<string>> GetToken(User user)
        {
            await _userRepository.Add(user);

            //Gera o Token           
            var token = await _tokenService.GenerateToken(user);

            // Retorna os dados
            return Ok(token);
        }

        [AllowAnonymous]
        [HttpPost("RefreshToken")]
        public async Task<ActionResult<string>> RefreshToken(TokenRequest token)
        {
            //Verifica o Token           
            var result = await _tokenService.VerifyAndGenerateToken(token);
            if (result == null)
            {
                return BadRequest("Invalid Token!");
            }

            //Retorna os dados
            return Ok(result);
        }

        [Authorize]
        [HttpPost("teste")]
        public ActionResult<string> Teste(User user)
        {
            if (_userRepository.Get(user.Id.ToString()!) == null)
                return BadRequest("Não autorizado!");

            return Ok("Autorizado!");
        }
    }
}