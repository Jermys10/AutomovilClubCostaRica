using AutomovilClub.Backend.Data;
using AutomovilClub.Backend.Enums;
using AutomovilClub.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AutomovilClub.Backend.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataContext _context;

        public HomeController(ILogger<HomeController> logger, DataContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var dataContext = _context.RequestLicenceSportInternational.Where(e => e.FullApproved == false && e.FullRejection == false && e.UserId==null).Include(r => r.DomicileCountry).Include(r => r.ResidenceCountry).Include(u => u.User);
                //var dataContext = _context.RequestLicenceSportInternational
                //.Where(e => e.FullApproved == false && e.FullRejection == false
                //    && e.DomicileCountry != null
                //    && e.ResidenceCountry != null
                //    && e.User != null)
                //.Include(r => r.DomicileCountry)
                //.Include(r => r.ResidenceCountry)
                //.Include(u => u.User);

                //var list = await _context.RequestLicenceSportInternational.ToListAsync();



                HomeViewModel homeViewModel = new HomeViewModel
                {
                    RequestLicenceSportInternationals = await dataContext.ToListAsync(),
                    RequestLicenceSports = await _context.RequestLicenceSports.Where(e => e.FullApproved == false && e.FullRejection == false && e.UserId == null).Include(u => u.User).ToListAsync(),
                    RequestAssociateMemberships = await _context.RequestAssociateMemberships.Where(e => e.FullApproved == false && e.FullRejection == false && e.UserId == null).Include(u => u.User).ToListAsync(),
                    RequestVirtualSportsOfficialLicenses = await _context.RequestVirtualSportsOfficialLicenses.Where(e => e.FullApproved == false && e.FullRejection == false && e.UserId == null).Include(u => u.User).ToListAsync(),
                    RequestLicenceConcursanteSports = await _context.RequestLicenceConcursanteSports.Where(e => e.FullApproved == false && e.FullRejection == false && e.UserId == null).Include(u => u.User).ToListAsync()
                };

                return View(homeViewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex.Message.ToString());
                return View(new HomeViewModel());
            }
        }

        public async Task<IActionResult> ChangeAssigned(int? id, int? formId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var model = new RequestViewModelShortH();

            switch (formId)
            {
                case 1:
                    var request1 = await _context.RequestLicenceSports.FindAsync(id);
                    if (request1 == null)
                    {
                        return NotFound();
                    }
                    model.RequestId = id.Value;
                    model.UserId = request1.UserId;
                    model.FormId = 1;
                    break;
                case 2:
                    var request2 = await _context.RequestLicenceConcursanteSports.FindAsync(id);
                    if (request2 == null)
                    {
                        return NotFound();
                    }
                    model.RequestId = id.Value;
                    model.UserId = request2.UserId;
                    model.FormId = 2;
                    break;
                case 3:
                    var request3 = await _context.RequestLicenceSportInternational.FindAsync(id);
                    if (request3 == null)
                    {
                        return NotFound();
                    }
                    model.RequestId = id.Value;
                    model.UserId = request3.UserId;
                    model.FormId = 3;
                    break;
                case 4:
                    var request4 = await _context.RequestAssociateMemberships.FindAsync(id);
                    if (request4 == null)
                    {
                        return NotFound();
                    }
                    model.RequestId = id.Value;
                    model.UserId = request4.UserId;
                    model.FormId = 4;
                    break;
                case 5:
                    var request5 = await _context.RequestVirtualSportsOfficialLicenses.FindAsync(id);
                    if (request5 == null)
                    {
                        return NotFound();
                    }
                    model.RequestId = id.Value;
                    model.UserId = request5.UserId;
                    model.FormId = 5;
                    break;
                default:
                    break;
            }         

            ViewData["UserId"] = new SelectList(
                  _context.Users.Where(u=>!u.IsDelete)
                      .AsEnumerable()  // Cambia a operaciones de cliente en memoria                      
                      .ToList(),
                  "Id",
                  "Name"
              );

            return View("_ChangeAssigned", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAssigned(RequestViewModelShortH model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.UserId != null)
                    {
                        var user = await _context.Users.Where(u => u.Id == model.UserId).FirstOrDefaultAsync();

                        if (user != null)
                        {
                            var formId = model.FormId;
                            var id = model.RequestId;

                            switch (formId)
                            {
                                case 1:
                                    var request1 = await _context.RequestLicenceSports.FindAsync(id);
                                    if (request1 == null)
                                    {
                                        return NotFound();
                                    }
                                    request1.UserId = user.Id;
                                    _context.Update(request1);
                                    await _context.SaveChangesAsync();
                                    break;
                                case 2:
                                    var request2 = await _context.RequestLicenceConcursanteSports.FindAsync(id);
                                    if (request2 == null)
                                    {
                                        return NotFound();
                                    }
                                    request2.UserId = user.Id;
                                    _context.Update(request2);
                                    await _context.SaveChangesAsync();
                                    break;
                                case 3:
                                    var request3 = await _context.RequestLicenceSportInternational.FindAsync(id);
                                    if (request3 == null)
                                    {
                                        return NotFound();
                                    }
                                    request3.UserId = user.Id;
                                    _context.Update(request3);
                                    await _context.SaveChangesAsync();
                                    break;
                                case 4:
                                    var request4 = await _context.RequestAssociateMemberships.FindAsync(id);
                                    if (request4 == null)
                                    {
                                        return NotFound();
                                    }
                                    request4.UserId = user.Id;
                                    _context.Update(request4);
                                    await _context.SaveChangesAsync();
                                    break;
                                case 5:
                                    var request5 = await _context.RequestVirtualSportsOfficialLicenses.FindAsync(id);
                                    if (request5 == null)
                                    {
                                        return NotFound();
                                    }
                                    request5.UserId = user.Id;
                                    _context.Update(request5);
                                    await _context.SaveChangesAsync();
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(
                 _context.Users
                     .AsEnumerable()  // Cambia a operaciones de cliente en memoria                      
                     .ToList(),
                 "Id",
                 "Name"
             );

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
