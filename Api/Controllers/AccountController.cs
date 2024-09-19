using Api.Dto.Account;
using Api.DTO.Account;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Validations;
using System.ComponentModel;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly JWTService jwtService;
        private readonly SignInManager<User> signInManager;
        private readonly UserManager<User> userManager;
        private readonly EmailService emailService;
        private readonly IConfiguration _config;

        public AccountController
            (JWTService jwtService,
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            EmailService emailService,
            IConfiguration config)
            
        {
            this.jwtService = jwtService;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.emailService = emailService;
            this._config = config;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto model)
        {

            var user = await this.userManager.FindByNameAsync(model.UserName);

            if (user == null) return Unauthorized("Invalid username or password");
            if (user.EmailConfirmed == false) return Unauthorized("please confirm your email");
            var result = await this.signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded) return Unauthorized("Invalid username or password");
            return CreateApplicationUserDTO(user);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto model) 
        {
            if (await CheckEmailExistsAsync(model.Email)) { 
                return BadRequest($"An existing account is using {model.Email}, email address. please try with anohter email address");
            }
            var userToAdd = new User
            {

                FirstName = model.FirstName.ToLower(),
                LastName = model.LastName.ToLower(),
                UserName = model.Email.ToLower(),
                Email = model.Email.ToLower(),
                //EmailConfirmed = true   "because we send email to confirm this"
            };

            var result = await this.userManager.CreateAsync(userToAdd,model.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            try
            {
                if (await SendConfirmEMailAsync(userToAdd)) {

                    return Ok(new JsonResult(new
                    {
                        title = "Account Created",
                        message = "Your account has been created, please confirm your email address"
                    }));
                }
                return BadRequest("Failed to send email. Please contact admin");
            }
            catch (System.Exception)
            {

                return BadRequest("Failed to send email. Please contact adming");
            }

            //return Ok(new JsonResult(new
            //{
            //    title = "Account Created",
            //    message = "Your account has been created, you can login"
            //}));
           
        }

        [Authorize]
        [HttpGet("refresh-user-token")]
        public async Task<ActionResult<UserDto>> RefreshUserToken() {

            var user = await this.userManager.FindByNameAsync(User.FindFirst(ClaimTypes.Email)?.Value);
            return CreateApplicationUserDTO(user);
        }

        [HttpPut("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto model) {

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized("This email address has not been registered yet");
            if (user.EmailConfirmed == true) return BadRequest("your email was confirmed before. please login to your account");
            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(model.Token);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);
                var result = await userManager.ConfirmEmailAsync(user, decodedToken);
                if (result.Succeeded) {
                    return Ok(new JsonResult(new { title = "Email confirmed", message = "your email address is confirmed. you can login now" }));
                }
                return BadRequest("Invalid token. Please try again");
            }
            catch (System.Exception)
            {

                return BadRequest("Invalid token. Please try again");
            }
        }


        [HttpPost("resend-email-confirmation-link/{email}")]
        public async Task<IActionResult> ResendEmailConfirmationLink(string email) 
        {
            if (string.IsNullOrEmpty(email)) return BadRequest("Invalid email");
            var user = await userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized("this email address has not been registered yet");
            if (user.EmailConfirmed == true) return BadRequest("Your email address was confirmed before. please login to your account");

            try
            {
                if (await SendConfirmEMailAsync(user)) {

                    return Ok(new JsonResult(new { title = "Confirmation link sent", message = "please confirm your email address" }));
                }

                return BadRequest("Failed to send email. Please contact admin");
            }
            catch (System.Exception)
            {

                return BadRequest("Failed to send email. Please contact admin");
            }
        }

        [HttpPost("forgot-username-or-password/{email}")]
        public async Task<IActionResult> ForgotUsernameOrPassword(string email)
        {
            if (string.IsNullOrEmpty(email)) return BadRequest("Invalid email");
            var user = await userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized("this email address has not been registered yet");
            if (user.EmailConfirmed == false) return BadRequest("Please confirm your email address first.");

            try
            {
                if (await SendForgotUsernameOrPasswordEmail(user))
                {

                    return Ok(new JsonResult(new { title = "Forgot username or password email sent", message = "please, check your email" }));
                }

                return BadRequest("Failed to send email. Please contact admin");
            }
            catch (System.Exception)
            {

                return BadRequest("Failed to send email. Please contact admin");
            }
        }

        [HttpPut("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            var user = await this.userManager.FindByNameAsync(model.Email);

            if (user == null) return Unauthorized("this email address has not been registered yet");
            if (user.EmailConfirmed == false) return Unauthorized("please confirm your email");

            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(model.Token);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);
                var result = await userManager.ResetPasswordAsync(user, decodedToken,model.NewPassword);
                if (result.Succeeded)
                {
                    return Ok(new JsonResult(new { title = "Password reset success", message = "your password has been reseted" }));
                }
                return BadRequest("Invalid token. Please try again");
            }
            catch (System.Exception)
            {

                return BadRequest("Invalid token. Please try again");
            }
        }


        #region Private Helper Methods
        private UserDto CreateApplicationUserDTO(User user) {
            return new UserDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                JWT = this.jwtService.CreateJWT(user)
            };
        }

        private async Task<bool> CheckEmailExistsAsync(string email) {
            return await this.userManager.Users.AnyAsync(elm => elm.Email == email.ToLower());
        }
        #endregion

        private async Task<bool> SendConfirmEMailAsync(User user) { 
        
            var token = await this.userManager.GenerateEmailConfirmationTokenAsync(user); // ask to gpt to do similar without Identity framework
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = $"{_config["JWT:ClientUrl"]}/{_config["Email:ConfirmEmailPath"]}?token={token}&email={user.Email}";

            var body = $"<p>Hello:{user.FirstName} {user.LastName}</p>" +
                "<p>Please confirm your email address by clicking on the following link.</p>" +
                $"<p><a href=\"{url}\">Click here </a></p>" +
                "<p>Thank you,</p>" +
                $"<br>{_config["Email:ApplicationName"]}";
            var emailSend = new EmailSendDto(user.Email, "Confirm your email", body);

            return await emailService.SendEmailAsync(emailSend);

        }

        private async Task<bool> SendForgotUsernameOrPasswordEmail(User user) {

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = $"{_config["JWT:ClientUrl"]}/{_config["Email:ResetPasswordPath"]}?token={token}&email={user.Email}";

            var body = $"<p>Hello:{user.FirstName} {user.LastName}</p>" +
               $"<p>Username: {user.UserName}</p>" +
               "<p>In order to reset your password, please click on the follow link.</p>"+
               $"<p><a href=\"{url}\">Click here </a></p>" +
               "<p>Thank you,</p>" +
               $"<br>{_config["Email:ApplicationName"]}";

            var emailSend = new EmailSendDto(user.Email, "Forgot username or password", body);

            return await emailService.SendEmailAsync(emailSend);
        }
    }
}
