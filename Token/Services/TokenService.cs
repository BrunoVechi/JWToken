using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using API.Models;
using API.Interfaces;

namespace API.Services
{
    public class TokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository? _userRepository;
        public TokenService(IRefreshTokenRepository refreshTokenRepository, IUserRepository userRepository)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userRepository = userRepository;
        }
        public async Task<AuthResult> GenerateToken(User user)
        {
            // Desserializa Key do JSON
            Key code = new();
            using (StreamReader r = new(@"../Token/appsettings.json"))
            {
                string json = r.ReadToEnd();
                code = JsonSerializer.Deserialize<Key>(json)!;
            }
            var key = Encoding.ASCII.GetBytes(code.Value!);

            // Instancia manipulador de JWT
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {   
                    //Corpo do Token
                    new Claim("Id", user.Id.ToString()!),
                    new Claim(JwtRegisteredClaimNames.Name, user.Name!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())

                }),

                // Tempo de Validade do Token
                Expires = DateTime.UtcNow.AddSeconds(30),

                // Instancia da chave de incriptação
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)

            };
            // Cria Token
            var token = tokenHandler.CreateToken(tokenDescriptor);
            // Escreve Token para string
            var jwtToken = tokenHandler.WriteToken(token);

            //Cria RefreshToken
            var refreshToken = new RefreshToken()
            {
                // Id do Token associado
                JwtId = token.Id,
                IsUsed = false,
                IsRevorked = false,
                // Id do User associado
                UserId = user.Id.ToString(),
                // Data de criação
                AddedDate = DateTime.UtcNow,
                // Data de expiração 
                ExpiryDate = DateTime.UtcNow.AddMinutes(1),
                // Codigo do Token
                Token = RandomString(5) + Guid.NewGuid()
            };

            // Salva no BD RefreshToken
            await _refreshTokenRepository.Add(refreshToken);

            // Retorna Token e RefreshToken
            return new AuthResult()
            {
                Token = jwtToken,
                RefreshToken = refreshToken.Token,
                Success = true
            };

        }
        public async Task<AuthResult> VerifyAndGenerateToken(TokenRequest tokenRequest)
        {
            try
            {
                // Desserializa Key do JSON
                Key code = new();
                using (StreamReader r = new(@"../Token/appsettings.json"))
                {
                    string json = r.ReadToEnd();
                    code = JsonSerializer.Deserialize<Key>(json)!;
                }
                var key = Encoding.ASCII.GetBytes(code.Value!);

                // Instancia manipulador de JWT
                var tokenHandler = new JwtSecurityTokenHandler();

                //Parametros de Validação do Token
                var tokenValidationParameters = new TokenValidationParameters
                {
                    //Valida Decriptação
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(code.Value!)),
                    //Valida emissor
                    ValidateIssuer = false,
                    //Valida receptor
                    ValidateAudience = false,
                    // Não Verificar Tempo de Expiração
                    ValidateLifetime = false
                };

                // 1 - Validation JWT Format
                var TokenInVerification = tokenHandler.ValidateToken(tokenRequest.Token, tokenValidationParameters, out var validatedToken);

                // 2 - Validation Encription Algorithms
                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

                    if (!result)
                        return null!;
                }

                // 3 - Validation Expiry Date              
                var utcExpireDate = long.Parse(TokenInVerification.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)!.Value);
                var expireDate = TimeStampToDateTime(utcExpireDate);
                if (expireDate > DateTime.Now)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Erros = new List<string>
                        {
                            "Token not yet expired"
                        }
                    };
                }

                //---- VALIDAÇÕES REFRESH TOKEN ----

                // 4 - Validation Existence in DB
                var storedToken = _refreshTokenRepository!.Get(tokenRequest.RefreshToken!);
                if (storedToken == null)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Erros = new List<string>
                        {
                            "RefreshToken does not exist"
                        }
                    };
                }

                // 5 - Validation if Used 
                if (storedToken.IsUsed)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Erros = new List<string>
                        {
                            "Token has been used"
                        }
                    };
                }

                // 6 - Validation if Revoked
                if (storedToken.IsRevorked)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Erros = new List<string>
                        {
                            "Token has been revoked"
                        }
                    };
                }

                // 7 - Validation Id
                var jti = TokenInVerification.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)!.Value;
                if (storedToken.JwtId != jti)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Erros = new List<string>
                        {
                            "Token doesn't match"
                        }
                    };
                }

                // 8 - Validation Expiry Date
                if (storedToken.ExpiryDate < DateTime.UtcNow)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Erros = new List<string>
                        {
                            "Refresh Token expired"
                        }
                    };
                }

                // Update current Refresh Token to Used
                storedToken.IsUsed = true;
                await _refreshTokenRepository.Update(storedToken);

                // Generate a new Token and Refresh Token
                var user = _userRepository!.Get(storedToken.UserId!);
                return await GenerateToken(user);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        private static string RandomString(int length)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWZYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(c => c[random.Next(c.Length)]).ToArray());
        }
        private static DateTime TimeStampToDateTime(long timeStamp)
        {
            var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTimeVal = dateTimeVal.AddSeconds(timeStamp).ToLocalTime();
            return dateTimeVal;
        }
    }
}
