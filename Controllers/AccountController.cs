namespace AutomovilClub.Backend.Controllers
{
    using AutomovilClub.Backend.Data;
    using AutomovilClub.Backend.Data.Entities;
    using AutomovilClub.Backend.Helpers;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Models;
    using System.Linq;
    using System.Threading.Tasks;

    public class AccountController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IUserHelper userHelper;
        private readonly IMailHelper mailHelper;
        private readonly IConfiguration configuration;
        private readonly DataContext _context;

        public AccountController(
            UserManager<User> userManager,
            IUserHelper userHelper,
            IMailHelper mailHelper,
            IConfiguration configuration,
            DataContext context)
        {
            this.userManager = userManager;
            this.userHelper = userHelper;
            this.mailHelper = mailHelper;
            this.configuration = configuration;
            _context = context;
        }

        public IActionResult Login()
        {
            if (this.User.Identity.IsAuthenticated)
            {
                return this.RedirectToAction("Index", "Home");
            }

            return this.View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                Microsoft.AspNetCore.Identity.SignInResult result = await this.userHelper.LoginAsync(model);
                if (result.Succeeded)
                {

                    var user = await this.userManager.FindByNameAsync(model.Username);
                    //user role list here
                    var roles = await this.userManager.GetRolesAsync(user);
                    //get default role here
                    string role = roles.FirstOrDefault();
                    if (role.Equals("Administrador"))
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else if (role.Equals("Usuario"))
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        //do somthing here.put in your logic 
                    }

                    if (Request.Query.Keys.Contains("ReturnUrl"))
                    {
                        return Redirect(Request.Query["ReturnUrl"].First());
                    }

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await this.userHelper.LogoutAsync();
            return this.RedirectToAction("Index", "Home");
        }

        //public IActionResult Register()
        //{
        //    var model = new RegisterNewUserViewModel
        //    {
        //        Countries = this.countryRepository.GetComboCountries(),
        //        Cities = this.countryRepository.GetComboCities(0)
        //    };

        //    return this.View(model);
        //}

        ////[HttpPost]
        ////public async Task<IActionResult> Register(RegisterNewUserViewModel model)
        ////{
        ////    if (this.ModelState.IsValid)
        ////    {
        ////        var user = await this.userHelper.GetUserByEmailAsync(model.Username);
        ////        if (user == null)
        ////        {
        ////            var city = await this.countryRepository.GetCityAsync(model.CityId);

        ////            user = new User
        ////            {
        ////                FirstName = model.FirstName,
        ////                LastName = model.LastName,
        ////                Email = model.Username,
        ////                UserName = model.Username,
        ////                Address = model.Address,
        ////                PhoneNumber = model.PhoneNumber,
        ////                CityId = model.CityId,
        ////                City = city
        ////            };

        ////            var result = await this.userHelper.AddUserAsync(user, model.Password);
        ////            if (result != IdentityResult.Success)
        ////            {
        ////                this.ModelState.AddModelError(string.Empty, "The user couldn't be created.");
        ////                return this.View(model);
        ////            }

        ////            var myToken = await this.userHelper.GenerateEmailConfirmationTokenAsync(user);
        ////            var tokenLink = this.Url.Action("ConfirmEmail", "Account", new
        ////            {
        ////                userid = user.Id,
        ////                token = myToken
        ////            }, protocol: HttpContext.Request.Scheme);

        ////            this.mailHelper.SendMail(model.Username, "Shop Email confirmation", $"<h1>Shop Email Confirmation</h1>" +
        ////                $"To allow the user, " +
        ////                $"plase click in this link:</br></br><a href = \"{tokenLink}\">Confirm Email</a>");
        ////            this.ViewBag.Message = "The instructions to allow your user has been sent to email.";
        ////            return this.View(model);
        ////        }

        ////        this.ModelState.AddModelError(string.Empty, "The username is already registered.");
        ////    }

        ////    return this.View(model);
        ////}

        ////public async Task<IActionResult> ChangeUser()
        ////{
        ////    var user = await this.userHelper.GetUserByEmailAsync(this.User.Identity.Name);
        ////    var model = new ChangeUserViewModel();

        ////    if (user != null)
        ////    {
        ////        model.FirstName = user.FirstName;
        ////        model.LastName = user.LastName;
        ////        model.Address = user.Address;
        ////        model.PhoneNumber = user.PhoneNumber;

        ////        var city = await this.countryRepository.GetCityAsync(user.CityId);
        ////        if (city != null)
        ////        {
        ////            var country = await this.countryRepository.GetCountryAsync(city);
        ////            if (country != null)
        ////            {
        ////                model.CountryId = country.Id;
        ////                model.Cities = this.countryRepository.GetComboCities(country.Id);
        ////                model.Countries = this.countryRepository.GetComboCountries();
        ////                model.CityId = user.CityId;
        ////            }
        ////        }
        ////    }

        ////    model.Cities = this.countryRepository.GetComboCities(model.CountryId);
        ////    model.Countries = this.countryRepository.GetComboCountries();
        ////    return this.View(model);
        ////}

        ////[HttpPost]
        ////public async Task<IActionResult> ChangeUser(ChangeUserViewModel model)
        ////{
        ////    if (this.ModelState.IsValid)
        ////    {
        ////        var user = await this.userHelper.GetUserByEmailAsync(this.User.Identity.Name);
        ////        if (user != null)
        ////        {

        ////            user.Name = model.FirstName;
        ////            user.LastName = model.LastName;
        ////            user.Address = model.Address;
        ////            user.PhoneNumber = model.PhoneNumber;

        ////            var respose = await this.userHelper.UpdateUserAsync(user);
        ////            if (respose.Succeeded)
        ////            {
        ////                this.ViewBag.UserMessage = "User updated!";
        ////            }
        ////            else
        ////            {
        ////                this.ModelState.AddModelError(string.Empty, respose.Errors.FirstOrDefault().Description);
        ////            }
        ////        }
        ////        else
        ////        {
        ////            this.ModelState.AddModelError(string.Empty, "User no found.");
        ////        }
        ////    }

        ////    return this.View(model);
        ////}

        //public IActionResult ChangePassword()
        //{
        //    return this.View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        //{
        //    if (this.ModelState.IsValid)
        //    {
        //        var user = await this.userHelper.GetUserByEmailAsync(this.User.Identity.Name);
        //        if (user != null)
        //        {
        //            var result = await this.userHelper.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        //            if (result.Succeeded)
        //            {
        //                return this.RedirectToAction("ChangeUser");
        //            }
        //            else
        //            {
        //                this.ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault().Description);
        //            }
        //        }
        //        else
        //        {
        //            this.ModelState.AddModelError(string.Empty, "User no found.");
        //        }
        //    }

        //    return this.View(model);
        //}

        //[HttpPost]
        //public async Task<IActionResult> CreateToken([FromBody] LoginViewModel model)
        //{
        //    if (this.ModelState.IsValid)
        //    {
        //        var user = await this.userHelper.GetUserByEmailAsync(model.Username);
        //        if (user != null)
        //        {
        //            var result = await this.userHelper.ValidatePasswordAsync(
        //                user,
        //                model.Password);

        //            if (result.Succeeded)
        //            {
        //                var claims = new[]
        //                {
        //                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        //                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        //                };

        //                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.configuration["Tokens:Key"]));
        //                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        //                var token = new JwtSecurityToken(
        //                    this.configuration["Tokens:Issuer"],
        //                    this.configuration["Tokens:Audience"],
        //                    claims,
        //                    expires: DateTime.UtcNow.AddDays(15),
        //                    signingCredentials: credentials);
        //                var results = new
        //                {
        //                    token = new JwtSecurityTokenHandler().WriteToken(token),
        //                    expiration = token.ValidTo
        //                };

        //                return this.Created(string.Empty, results);
        //            }
        //        }
        //    }

        //    return this.BadRequest();
        //}

        //public IActionResult NotAuthorized()
        //{
        //    return this.View();
        //}



        //public async Task<IActionResult> ConfirmEmail(string userId, string token)
        //{
        //    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        //    {
        //        return this.NotFound();
        //    }

        //    var user = await this.userHelper.GetUserByIdAsync(userId);
        //    if (user == null)
        //    {
        //        return this.NotFound();
        //    }

        //    var result = await this.userHelper.ConfirmEmailAsync(user, token);
        //    if (!result.Succeeded)
        //    {
        //        return this.NotFound();
        //    }

        //    user.EmailConfirmed = true;
        //    _context.Update(user);
        //    await _context.SaveChangesAsync();

        //    return View();
        //}

        //public IActionResult RecoverPasswordAdmin()
        //{
        //    this.ViewBag.Message = string.Empty;
        //    return this.View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> RecoverPasswordAdmin(RecoverPasswordViewModel model)
        //{
        //    if (this.ModelState.IsValid)
        //    {
        //        var user = await this.userHelper.GetUserByEmailAsync(model.Email);
        //        if (user == null)
        //        {
        //            ModelState.AddModelError(string.Empty, "The email doesn't correspont to a registered user.");
        //            return this.View(model);
        //        }

        //        var myToken = await this.userHelper.GeneratePasswordResetTokenAsync(user);
        //        var link = this.Url.Action("ResetPassword", "Account", new { token = myToken }, protocol: HttpContext.Request.Scheme);
        //        this.mailHelper.SendMail(user.Email, "Resetear de Contraseña ProfeCe", $"<h1>Recuperar Contraseña</h1>" +
        //              $"Para restablecer la contraseña haz click en este enlace:</br></br>" +
        //              $"<a href = \"{link}\">Resetear Contraseña</a>");
        //        this.ViewBag.Message = "Las instrucciones para recuperar su contraseña han sido enviadas al correo electrónico.";
        //        return this.View();

        //    }

        //    return this.View(model);
        //}

        //public IActionResult ResetPassword(string token)
        //{
        //    this.ViewBag.Message = string.Empty;
        //    return View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        //{
        //    var user = await this.userHelper.GetUserByEmailAsync(model.UserName);
        //    if (user != null)
        //    {
        //        var result = await this.userHelper.ResetPasswordAsync(user, model.Token, model.Password);
        //        if (result.Succeeded)
        //        {
        //            this.ViewBag.Message = "Restablecimiento de contraseña exitosa, vuelve al App e inicia sesión con tu nueva contraseña.";
        //            return this.View();
        //        }

        //        this.ViewBag.Message = "\r\nError al restablecer la contraseña.";
        //        return View(model);
        //    }

        //    this.ViewBag.Message = "Usuario no encontrado.";
        //    return View(model);
        //}

        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> Index()
        //{
        //    var users = await this.userHelper.GetAllUsersAsync();
        //    foreach (var user in users)
        //    {
        //        var myUser = await this.userHelper.GetUserByIdAsync(user.Id);
        //        if (myUser != null)
        //        {
        //            user.IsAdmin = await this.userHelper.IsUserInRoleAsync(myUser, "Admin");
        //        }
        //    }

        //    return this.View(users);
        //}


        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> AdminOff(string id)
        //{
        //    if (string.IsNullOrEmpty(id))
        //    {
        //        return NotFound();
        //    }

        //    var user = await this.userHelper.GetUserByIdAsync(id);
        //    if (user == null)
        //    {
        //        return NotFound();
        //    }

        //    await this.userHelper.RemoveUserFromRoleAsync(user, "Admin");
        //    return this.RedirectToAction(nameof(Index));
        //}

        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> AdminOn(string id)
        //{
        //    if (string.IsNullOrEmpty(id))
        //    {
        //        return NotFound();
        //    }

        //    var user = await this.userHelper.GetUserByIdAsync(id);
        //    if (user == null)
        //    {
        //        return NotFound();
        //    }

        //    await this.userHelper.AddUserToRoleAsync(user, "Admin");
        //    return this.RedirectToAction(nameof(Index));
        //}

        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> DeleteUser(string id)
        //{
        //    if (string.IsNullOrEmpty(id))
        //    {
        //        return NotFound();
        //    }

        //    var user = await this.userHelper.GetUserByIdAsync(id);
        //    if (user == null)
        //    {
        //        return NotFound();
        //    }

        //    await this.userHelper.DeleteUserAsync(user);
        //    return this.RedirectToAction(nameof(Index));
        //}
    }
}
