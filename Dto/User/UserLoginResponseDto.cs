namespace Dto.User
{
    public class UserLoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto? User { get; set; } = null;
    }
}
