namespace API.Models
{
    public class AuthResult
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public bool Success { get; set; }
        public List<string>? Erros { get; set; }

    }
}
