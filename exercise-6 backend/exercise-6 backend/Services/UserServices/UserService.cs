using System.Security.Claims;

namespace exercise_6_backend.Services.UserServices
{
    public class UserService:IUserInterface
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserService( IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }


        public string getMyName()
        {
            var result = _httpContextAccessor != null ?
                _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name) :
                string.Empty;
            return result;
        }
    }
}
