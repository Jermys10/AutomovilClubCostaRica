using AutomovilClub.Backend.Data;
using AutomovilClub.Backend.Data.Entities;
using AutomovilClub.Backend.Enums;
using AutomovilClub.Backend.Helpers;
using AutomovilClub.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CFV.Backend.Controllers
{
    public class UsersController : Controller
    {
        private readonly DataContext _context;
        private readonly IUserHelper _userHelper;
        private readonly IConverterHelper _converterHelper;
        private readonly IMailHelper _mailHelper;
        private readonly IFilesHelper _filesHelper;

        public UsersController(DataContext context, IUserHelper userHelper,
       IConverterHelper converterHelper, IMailHelper mailHelper, IFilesHelper filesHelper)
        {
            _context = context;
            _userHelper = userHelper;
            _converterHelper = converterHelper;
            _mailHelper = mailHelper;
            _filesHelper = filesHelper;
        }

        public async Task<IActionResult> Index()
        {


            List<User> list = await _context.Users.Where(u => !u.IsDelete)
                .ToListAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> UserAssigned()
        {
            DateTime startDate = DateTime.Now.AddDays(-30);
            DateTime endDate = DateTime.Now;

            
            List<User> list = await _context.Users.Where(u => !u.IsDelete)
                .Include(u => u.RequestLicenceSports.Where(l => (l.Create >= startDate) && l.Create <= endDate))
                .Include(u => u.RequestLicenceConcursanteSports.Where(l => (l.Create >= startDate) && l.Create <= endDate))
                .Include(u => u.RequestLicenceSportInternationals.Where(l => (l.Create >= startDate) && l.Create <= endDate))
                .Include(u => u.RequestAssociateMemberships.Where(l => (l.Create >= startDate) && l.Create <= endDate))
                .Include(u => u.RequestVirtualSportsOfficialLicenses.Where(l => (l.Create >= startDate) && l.Create <= endDate))
                .ToListAsync();

            ViewBag.StartDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.ToString("yyyy-MM-dd");

            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> UserAssigned(DateTime? startDate, DateTime? endDate)
        {

            // Ajustar la fecha de fin al final del día si tiene valor
            if (endDate.HasValue)
            {
                endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
            }

            List<User> list = await _context.Users.Where(u => !u.IsDelete)
                .Include(u => u.RequestLicenceSports.Where(l => (l.Create >= startDate) && l.Create <= endDate))
                .Include(u => u.RequestLicenceConcursanteSports.Where(l => (l.Create >= startDate) && l.Create <= endDate))
                .Include(u => u.RequestLicenceSportInternationals.Where(l => (l.Create >= startDate) && l.Create <= endDate))
                .Include(u => u.RequestAssociateMemberships.Where(l => (l.Create >= startDate) && l.Create <= endDate))
                .Include(u => u.RequestVirtualSportsOfficialLicenses.Where(l => (l.Create >= startDate) && l.Create <= endDate))
                .ToListAsync();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(list);
        }

        public IActionResult Create()
        {
            UserViewModel model = new UserViewModel();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var pic = string.Empty;
                var folder = "\\img\\users\\";

                User user = await _converterHelper.ToUserAsync(model, pic, true);
                user.Role = model.Role == 0 ? Role.Administrador : Role.Usuario;
                var result= await _userHelper.AddUserAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    return View(model);
                }
                await _userHelper.AddUserToRoleAsync(user, user.Role.ToString());

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            User user = await _userHelper.GetUserAsync(Guid.Parse(id));
            if (user == null)
            {
                return NotFound();
            }

            UserViewModel model = _converterHelper.ToUserViewModel(user);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserViewModel model)
        {            
            if (ModelState.IsValid)
            {
                var pic = model.Image;
                var folder = "\\img\\users\\";

                User user = await _converterHelper.ToUserAsync(model, pic, false);
                await _userHelper.UpdateUserAsync(user);
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Ok(new Response()
                {
                    IsSuccess = false,
                    Message = "Se requiere un Id de Usuario",
                    Result= null
                });
            }

            User user = await _userHelper.GetUserAsync(Guid.Parse(id));
            if (user == null)
            {
                return Ok(new Response()
                {
                    IsSuccess = false,
                    Message = "El usuario que busca no existe",
                    Result = null
                });
            }

            //await _blobHelper.DeleteBlobAsync(user.ImageId, "users");
            user.IsDelete = true;
            await _userHelper.UpdateUserAsync(user);
            return RedirectToAction(nameof(Index));
            //return RedirectToAction(new Response()
            //{
            //    IsSuccess = true,
            //    Message = null,
            //    Result = user
            //});
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            User user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }
    }
}
