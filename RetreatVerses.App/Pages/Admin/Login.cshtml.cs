using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace RetreatVerses.App.Pages.Admin
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(string? username, string? password, string? returnUrl)
        {
            var expectedUser = _configuration["Admin:Username"];
            var expectedPass = _configuration["Admin:Password"];

            if (string.IsNullOrWhiteSpace(expectedUser) || string.IsNullOrWhiteSpace(expectedPass))
            {
                ErrorMessage = "관리자 계정이 설정되지 않았습니다.";
                return Page();
            }

            if (username != expectedUser || password != expectedPass)
            {
                ErrorMessage = "아이디 또는 비밀번호가 올바르지 않습니다.";
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, expectedUser),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "AdminCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("AdminCookie", principal, new AuthenticationProperties
            {
                IsPersistent = true
            });

            var redirect = string.IsNullOrWhiteSpace(returnUrl) ? "/admin" : returnUrl;
            return LocalRedirect(redirect);
        }
    }
}
