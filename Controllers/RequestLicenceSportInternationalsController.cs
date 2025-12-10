using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AutomovilClub.Backend.Data;
using AutomovilClub.Backend.Data.Entities;
using AutomovilClub.Backend.Models;
using AutomovilClub.Backend.Helpers;
using Microsoft.AspNetCore.Hosting;
using IronOcr;
using IronSoftware.Drawing;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;
using System.Drawing;
using AutomovilClub.Backend.Enums;

namespace AutomovilClub.Backend.Controllers
{
    public class RequestLicenceSportInternationalsController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMailHelper _mailHelper;
        private readonly IConfiguration _configuration;
        private readonly Data.Entities.Configuration _configurationApp;

        public RequestLicenceSportInternationalsController(DataContext context,
            IWebHostEnvironment webHostEnvironment,
            IMailHelper mailHelper,
            IConfiguration configuration)
        {
            _webHostEnvironment = webHostEnvironment;
            _mailHelper = mailHelper;
            _context = context;
            _configuration = configuration;

            var configuraciones = _context.Configurations.ToList();

            if (configuraciones.Count > 0)
            {
                _configurationApp = configuraciones[0];
            }
        }

        // GET: RequestLicenceSportInternationals
        public async Task<IActionResult> Index(int? filterTypeId = 0, string? userFilterId = "-1")
        {
            var dataContext = await _context.RequestLicenceSportInternational.Include(u => u.User).Include(r => r.DomicileCountry).Include(r => r.ResidenceCountry).ToListAsync();


            if (userFilterId != null && userFilterId != "-1")
            {
                dataContext = dataContext
                                .Where(r => r.UserId == userFilterId).ToList();
            }


            if (filterTypeId == 0)
            {
                dataContext = dataContext
                .Where(r => r.FullRejection == false
                     && r.FullApproved == false).ToList();
            }

            if (filterTypeId == 1)
            {
                dataContext = dataContext
                                   .Where(r => r.FullRejection == false
                                                       && r.FullApproved == true).ToList();
            }

            if (filterTypeId == 2)
            {
                dataContext = dataContext
                                                      .Where(r => r.FullRejection == true
                                                                                                            && r.FullApproved == false).ToList();
            }

            ViewData["UserId"] = new SelectList(
                 new[] { new { Id = "-1", Name = "Todos" } }
                 .Concat(_context.Users
                     .AsEnumerable() // Operación en memoria
                     .Select(user => new { Id = user.Id.ToString(), Name = user.Name }))
                 .ToList(),
                 "Id",
                 "Name",
                 userFilterId
             );


            ViewBag.FilterTypeId = filterTypeId;
    
            return View(dataContext.ToList());
        }

        public async Task<IActionResult> ChangeAssigned(int? id, int? filterTypeId, int? filterLicenceType)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSport = await _context.RequestLicenceSportInternational.FindAsync(id);
            if (requestLicenceSport == null)
            {
                return NotFound();
            }

            ViewData["UserId"] = new SelectList(
                  _context.Users.Where(u => !u.IsDelete)
                      .AsEnumerable()  // Cambia a operaciones de cliente en memoria                      
                      .ToList(),
                  "Id",
                  "Name"
              );


            var model = new RequestViewModelShort()
            {
                RequestId = requestLicenceSport.RequestLicenceSportInternationalId,
                UserId = requestLicenceSport.UserId,
                FilterLicenceType = filterLicenceType,
                FilterTypeId = filterTypeId
            };

            return View("_ChangeAssigned", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAssigned(RequestViewModelShort model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var request = await _context.RequestLicenceSportInternational.FindAsync(model.RequestId);

                    if (model.UserId != null)
                    {
                        var user = await _context.Users.Where(u => u.Id == model.UserId).FirstOrDefaultAsync();

                        if (user != null)
                        {
                            request.UserId = user.Id;
                        }
                    }

                    _context.Update(request);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(Index), new { model.FilterTypeId, model.FilterLicenceType });
            }
            var licenceTypes = Enum.GetValues(typeof(LicenceType))
                               .Cast<LicenceType>()
                               .Select(s => new SelectListItem
                               {
                                   Value = ((int)s).ToString(),
                                   Text = s.ToString()
                               })
                               .ToList();

            ViewBag.LicenceTypes = licenceTypes;

            return View(model);
        }

        public async Task<IActionResult> FindPerson(string identification)
        {
            var person = _context.People.Where(p => p.Identification == identification).FirstOrDefault();

            if (person == null)
            {
                return Ok(new {succes=false, message="No existe"});
            }

            return Ok(new { succes = true, data=person });
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSportInternational = await _context.RequestLicenceSportInternational
                .Include(r => r.DomicileCountry)
                .Include(r => r.ResidenceCountry)
                .FirstOrDefaultAsync(m => m.RequestLicenceSportInternationalId == id);
            if (requestLicenceSportInternational == null)
            {
                return NotFound();
            }

            return View(requestLicenceSportInternational);
        }


        public IActionResult Create()
        {
            ViewData["DomicileCountryId"] = new SelectList(_context.Countries, "CountryId", "Name");
            ViewData["ResidenceCountryId"] = new SelectList(_context.Countries, "CountryId", "Name");
            return View(new RequestLicenceSportInterantionalViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RequestLicenceSportInterantionalViewModel model)
        {

            var contentType = model.PhotoFile.ContentType.ToLowerInvariant();
            if (!contentType.StartsWith("image/"))
            {
                ModelState.AddModelError("PhotoFile", "Solo se permiten archivos de imagen (JPG, PNG, etc).");
                // volver con el modelo a la vista
                // Crear la SelectList a partir del enum
                ViewData["DomicileCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.DomicileCountryId);
                ViewData["ResidenceCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.ResidenceCountryId);
                return View(model);
            }

            try
            {
                using var image = System.Drawing.Image.FromStream(model.PhotoFile.OpenReadStream());
            }
            catch
            {
                ModelState.AddModelError("PhotoFile", "La imagen no es válida o está dañada.");
                // Crear la SelectList a partir del enum
                ViewData["DomicileCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.DomicileCountryId);
                ViewData["ResidenceCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.ResidenceCountryId);
                return View(model);
            }


            if (ModelState.IsValid)
            {
                var guid = Guid.NewGuid();
                var identificacionFPArchivo = "IFP_" + guid + ".jpg";
                var identificacionTPArchivo = "ITP_" + guid + ".jpg";
                var licenciaFPArchivo = "LFP_" + guid + ".jpg";
                var licenciaTPArchivo = "LTP_" + guid + ".jpg";
                var nombreArchivoPhoto = "PH_" + guid + ".jpg";

                if (model.PhotoFile==null)
                {
                    ModelState.AddModelError(string.Empty, "La foto es requerida");
                    ViewData["DomicileCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.DomicileCountryId);
                    ViewData["ResidenceCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.ResidenceCountryId);
                    return View(model);
                }

                if (model.IdentificationFPFile==null || model.IdentificationTPFile == null)
                {
                    ModelState.AddModelError(string.Empty, "La identificación es requerida");
                    ViewData["DomicileCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.DomicileCountryId);
                    ViewData["ResidenceCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.ResidenceCountryId);
                    return View(model);
                }     

                if (model.LicenceFPFile == null || model.LicenceFPFile == null)
                {
                    ModelState.AddModelError(string.Empty, "La licencia es requerida");
                    ViewData["DomicileCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.DomicileCountryId);
                    ViewData["ResidenceCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.ResidenceCountryId);
                    return View(model);
                }

                var identificationFPPath = await GuardarArchivoEnServidor(model.IdentificationFPFile, @"files\images\" + identificacionFPArchivo);
                var identificationTPPath = await GuardarArchivoEnServidor(model.IdentificationTPFile, @"files\images\" + identificacionTPArchivo);

                var licenceFPPath = string.Empty;
                if (model.LicenceFPFile != null)
                    licenceFPPath = await GuardarArchivoEnServidor(model.LicenceFPFile, @"files\images\" + licenciaFPArchivo);

                var licenceTPPath = string.Empty;
                if (model.LicenceTPFile != null)
                    licenceTPPath = await GuardarArchivoEnServidor(model.LicenceTPFile, @"files\images\" + licenciaTPArchivo);

                var imgPathPhoto = await GuardarArchivoEnServidor(model.PhotoFile, @"files\photos\" + nombreArchivoPhoto);

                if (_configurationApp.ValidateIdentification)
                {
                    var identificacionFPContiene = RealizarOCR(identificationFPPath);
                    var identificacionTPContiene = RealizarOCR(identificationTPPath);

                    var approvedIdentificacionFP = ContieneNumeroIdentificacion(identificacionFPContiene);

                    var approvedIdentificacionTP = VencimientoDocument(identificacionTPContiene);

                    if (!approvedIdentificacionTP)
                    {
                        approvedIdentificacionTP = ContieneNumeroIdentificacion(identificacionTPContiene);
                    }

                    if (approvedIdentificacionFP)
                    {
                        model.IdentificationFP = identificacionFPArchivo;
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "La identificación no es válida");
                        ViewData["DomicileCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.DomicileCountryId);
                        ViewData["ResidenceCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.ResidenceCountryId);
                        return View(model);
                    }

                    if (approvedIdentificacionTP)
                    {
                        model.IdentificationFP = identificacionFPArchivo;
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "La identificación no es válida, fecha de vencimiento");
                        ViewData["DomicileCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.DomicileCountryId);
                        ViewData["ResidenceCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.ResidenceCountryId);
                        return View(model);
                    }
                }

                var requestLicenceSport = new RequestLicenceSportInternational()
                {
                    DomicileCountryId = model.DomicileCountryId,
                    //Identification = model.Identification,
                    Name = model.Name,
                    Mail = model.Mail,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    Travel = model.Travel,
                    ResidenceCountryId = model.ResidenceCountryId,
                    Photo = model.PhotoFile!=null ? $"~/files/photos/{nombreArchivoPhoto}" : null,
                    IdentificationFP = model.IdentificationFPFile != null ? $"~/files/images/{identificacionFPArchivo}" : null,
                    IdentificationTP = model.IdentificationTPFile != null ? $"~/files/images/{identificacionTPArchivo}" : null,
                    LicenceFP = model.LicenceFPFile != null ? $"~/files/images/{licenciaFPArchivo}" : null,
                    LicenceTP = model.LicenceTPFile != null ? $"~/files/images/{licenciaTPArchivo}" : null,
                    DomicileCity = model.DomicileCity,
                    ResidenceCity = model.ResidenceCity,
                };

                _context.Add(requestLicenceSport);
                await _context.SaveChangesAsync();

                _mailHelper.SendMail(model.Mail, "Solicitud de permiso internacional", _configurationApp.MessagePermiso + GenerateHtmlTable(model, 0));

                _mailHelper.SendMail(_configuration["Mail:Admin"], "Solicitud de permiso internacional", _configurationApp.MessagePermisoAdmin + GenerateHtmlTable(model, 0));

                return RedirectToAction(nameof(Thanks));
            }
            ViewData["DomicileCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.DomicileCountryId);
            ViewData["ResidenceCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.ResidenceCountryId);
            return View(model);
        }


        public async Task<IActionResult> CreateImageAnverso(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestLicenceSportInternational.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaInternacional", "PERMISO-F-IMTERNACIONAL.jpg");
            var fotoRecienteUrl = requestLicence.PhotoFullPath;

            using (HttpClient client = new HttpClient())
            using (Stream fotoStream = await client.GetStreamAsync(fotoRecienteUrl))
            using (var fondo = System.Drawing.Image.FromFile(fondoImage))
            using (var foto = System.Drawing.Image.FromStream(fotoStream))
            using (var image = new Bitmap(fondo.Width, fondo.Height))
            using (var graphics = Graphics.FromImage(image))
            using (var memoryStream = new MemoryStream())
            {
                graphics.DrawImage(fondo, 0, 0); // Dibuja el fondo primero

                // Definir el área donde se colocará la foto de la persona
                System.Drawing.Rectangle photoRect = new System.Drawing.Rectangle(160, 50, 180, 230); // Ajustar estas coordenadas y tamaño según necesidades
                int cornerRadius = 20; // Radio de las esquinas redondeadas
                int borderSize = 5; // Tamaño del borde


                // Crear el rectángulo con esquinas redondeadas para el borde
                System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(photoRect.X, photoRect.Y, cornerRadius, cornerRadius, 180, 90);
                path.AddArc(photoRect.X + photoRect.Width - cornerRadius, photoRect.Y, cornerRadius, cornerRadius, 270, 90);
                path.AddArc(photoRect.X + photoRect.Width - cornerRadius, photoRect.Y + photoRect.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
                path.AddArc(photoRect.X, photoRect.Y + photoRect.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
                path.CloseFigure();

                // Llenar el área del path con color blanco para el fondo del rectángulo
                using (var fillBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                {
                    graphics.FillPath(fillBrush, path);
                }

                // Dibujar el borde redondeado
                using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.White, borderSize))
                {
                    graphics.DrawPath(borderPen, path);
                }

                // Ajustar el rectángulo para la foto, considerando el borde
                System.Drawing.Rectangle photoRectWithBorder = System.Drawing.Rectangle.Inflate(photoRect, -borderSize, -borderSize);

                // Dibujar la foto en la posición definida dentro del borde
                graphics.SetClip(path);
                graphics.DrawImage(foto, photoRectWithBorder);
                graphics.ResetClip(); // Remover el recorte

                // Define the text to be drawn on the image from the requestLicence object
                string nombreCompleto = requestLicence.Name; // Reemplazar con la propiedad real de requestLicence
                string numeroLicencia = ""; // Reemplazar con la propiedad real de requestLicence
                                                                      // ... y así para el resto de los campos que necesitas mostrar

                // Utiliza diferentes fuentes y tamaños según la necesidad
                using (var fontTitle = new System.Drawing.Font("Arial", 24, System.Drawing.FontStyle.Regular))
                using (var fontLabel = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold))
                using (var fontBodySmall = new System.Drawing.Font("Arial", 6, System.Drawing.FontStyle.Regular))
                using (var fontBody = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Regular))
                using (var fontBody2 = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 2))
                {
                    graphics.DrawString(nombreCompleto, fontTitle, brush, new System.Drawing.PointF(100, 300)); // Ajustar posición y fuente como sea necesario
                    graphics.DrawString("N.:", fontLabel, brush, new System.Drawing.PointF(20, 375));
                    graphics.DrawString(numeroLicencia, fontBody, brush, new System.Drawing.PointF(400, 375));
                    graphics.DrawString("GRADE:", fontLabel, brush, new System.Drawing.PointF(20, 405));
                    graphics.DrawString("NAE", fontBody, brush, new System.Drawing.PointF(400, 405));
                    graphics.DrawString("D.O.B:", fontLabel, brush, new System.Drawing.PointF(20, 435));
                    graphics.DrawString("--/--/----", fontBody, brush, new System.Drawing.PointF(400, 435));
                    graphics.DrawString("EXPEDITION:", fontLabel, brush, new System.Drawing.PointF(20, 465));
                    graphics.DrawString(DateTime.Now.Date.ToString("MM/dd/yyyy"), fontBody, brush, new System.Drawing.PointF(400, 465));
                    graphics.DrawString("EXPIRE:", fontLabel, brush, new System.Drawing.PointF(20, 495));
                    graphics.DrawString("12/31/" + DateTime.Now.Date.Year, fontBody, brush, new System.Drawing.PointF(400, 495));
                    // Añade más campos de texto de la misma manera

                    graphics.DrawString("C.EYE.", fontBody2, brush, new System.Drawing.PointF(20, 545));
                    graphics.DrawString("MS", fontBody2, brush, new System.Drawing.PointF(20, 570));
                    graphics.DrawString("", fontBody2, brush, new System.Drawing.PointF(20, 595));
                    graphics.DrawString("WADB", fontBody2, brush, new System.Drawing.PointF(20, 610));
                    graphics.DrawString("", fontBody2, brush, new System.Drawing.PointF(20, 625));
                    graphics.DrawString("FIA M.S", fontLabel, brush, new System.Drawing.PointF(20, 650));
                    graphics.DrawString(DateTime.Now.Date.ToString("MM/dd/yyyy"), fontBody, brush, new System.Drawing.PointF(400, 650));
                    graphics.DrawString("APT FOR THE PRACTICE OF MOTORS SPORT.ACCORDING", fontBody2, brush, new System.Drawing.PointF(100, 685));
                    graphics.DrawString("TO THE MEDICAL STANDARDS OF THE FIA.", fontBody2, brush, new System.Drawing.PointF(105, 700));

                    // Dimensiones y posición para los cajones "SI" y "NO"
                    int checkboxSize = 15; // Tamaño del lado del cajón
                    System.Drawing.Point checkboxSiPosition = new System.Drawing.Point(400, 545); // Ajustar posición
                    System.Drawing.Point checkboxNoPosition = new System.Drawing.Point(450, 545); // Ajustar posición

                    System.Drawing.Point checkboxSiPosition2 = new System.Drawing.Point(400, 570); // Ajustar posición
                    System.Drawing.Point checkboxNoPosition2 = new System.Drawing.Point(450, 570); // Ajustar posición

                    System.Drawing.Point checkboxSiPosition3 = new System.Drawing.Point(400, 610); // Ajustar posición
                    System.Drawing.Point checkboxNoPosition3 = new System.Drawing.Point(450, 610); // Ajustar posición


                    graphics.DrawString("Si", fontBodySmall, brush, new System.Drawing.PointF(400, 530));
                    graphics.DrawString("No", fontBodySmall, brush, new System.Drawing.PointF(450, 530));

                    // Crear cajón "SI"
                    System.Drawing.Rectangle checkboxSiRect = new System.Drawing.Rectangle(checkboxSiPosition.X, checkboxSiPosition.Y, checkboxSize, checkboxSize);
                    graphics.DrawRectangle(pen, checkboxSiRect);
                    graphics.FillRectangle(brush, checkboxSiRect);

                    // Crear cajón "NO", usando la posición X ajustada para colocarlo a la par
                    System.Drawing.Rectangle checkboxNoRect = new System.Drawing.Rectangle(checkboxNoPosition.X, checkboxNoPosition.Y, checkboxSize, checkboxSize);
                    graphics.DrawRectangle(pen, checkboxNoRect);
                    graphics.FillRectangle(brush, checkboxNoRect);

                    // Crear cajón "SI"
                    System.Drawing.Rectangle checkboxSiRect2 = new System.Drawing.Rectangle(checkboxSiPosition2.X, checkboxSiPosition2.Y, checkboxSize, checkboxSize);
                    graphics.DrawRectangle(pen, checkboxSiRect2);
                    graphics.FillRectangle(brush, checkboxSiRect2);

                    // Crear cajón "NO", usando la posición X ajustada para colocarlo a la par
                    System.Drawing.Rectangle checkboxNoRect2 = new System.Drawing.Rectangle(checkboxNoPosition2.X, checkboxNoPosition2.Y, checkboxSize, checkboxSize);
                    graphics.DrawRectangle(pen, checkboxNoRect2);
                    graphics.FillRectangle(brush, checkboxNoRect2);

                    // Crear cajón "SI"
                    System.Drawing.Rectangle checkboxSiRect3 = new System.Drawing.Rectangle(checkboxSiPosition3.X, checkboxSiPosition3.Y, checkboxSize, checkboxSize);
                    graphics.DrawRectangle(pen, checkboxSiRect3);
                    graphics.FillRectangle(brush, checkboxSiRect3);

                    // Crear cajón "NO", usando la posición X ajustada para colocarlo a la par
                    System.Drawing.Rectangle checkboxNoRect3 = new System.Drawing.Rectangle(checkboxNoPosition3.X, checkboxNoPosition3.Y, checkboxSize, checkboxSize);
                    graphics.DrawRectangle(pen, checkboxNoRect3);
                    graphics.FillRectangle(brush, checkboxNoRect3);
                }




                image.Save(memoryStream, ImageFormat.Png);
                return File(memoryStream.ToArray(), "image/png");
            }
        }



     

        public async Task<IActionResult> CreateImageReverso(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestLicenceSports.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaInternacional", "PERMISO-internacional.jpg");

            using (var fondo = System.Drawing.Image.FromFile(fondoImage))
            using (var image = new Bitmap(fondo.Width, fondo.Height))
            using (var graphics = Graphics.FromImage(image))
            using (var memoryStream = new MemoryStream())
            {
                graphics.DrawImage(fondo, 0, 0); // Dibuja el fondo primero
                image.Save(memoryStream, ImageFormat.Png);
                return File(memoryStream.ToArray(), "image/png");
            }
        }


        public async Task<IActionResult> CreateAAAAnverso(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestLicenceSportInternational.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/AAA", "AAA.jpg");
            var fotoRecienteUrl = requestLicence.PhotoFullPath;


            var pathLogo1 = Path.Combine(_webHostEnvironment.WebRootPath, "images/AAA", "660x74_AAA_discounts-&-rewards-UN_0.png");

            using (var logo1 = System.Drawing.Image.FromFile(pathLogo1))
            using (HttpClient client = new HttpClient())
            using (Stream fotoStream = await client.GetStreamAsync(fotoRecienteUrl))
            using (var fondo = System.Drawing.Image.FromFile(fondoImage))
            using (var foto = System.Drawing.Image.FromStream(fotoStream))
            using (var image = new Bitmap(fondo.Width, fondo.Height))
            using (var graphics = Graphics.FromImage(image))
            using (var memoryStream = new MemoryStream())
            {
                graphics.DrawImage(fondo, 0, 0); // Dibuja el fondo primero
                graphics.DrawImage(logo1, 30, 75, 450, 80);

               

                // Utiliza diferentes fuentes y tamaños según la necesidad
                using (var fontTitle = new System.Drawing.Font("Arial", 20, System.Drawing.FontStyle.Regular))
                using (var fontLabel = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold))
                using (var fontBodySmall = new System.Drawing.Font("Arial", 6, System.Drawing.FontStyle.Regular))
                using (var fontBody = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Regular))
                using (var fontBody2 = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                using (var brush2 = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 2))
                {
                    graphics.DrawString("When presented with a valid international club", fontLabel, brush, new System.Drawing.PointF(50, 150));
                    graphics.DrawString("membership card, this paper card entitles you to:", fontLabel, brush, new System.Drawing.PointF(50, 165));
                    graphics.DrawString("the AAA Discount & Rewards participating", fontLabel, brush, new System.Drawing.PointF(50, 180));
                    graphics.DrawString("partener offer accesible on AAA.com/International.", fontLabel, brush, new System.Drawing.PointF(50, 195));
                    graphics.DrawString("Offer are subject to restriction, chhange or", fontLabel, brush, new System.Drawing.PointF(50, 210));
                    graphics.DrawString("cancellation without prior notice.", fontLabel, brush, new System.Drawing.PointF(50, 225));


                    graphics.DrawString("International Member Discount Card", fontTitle, brush2, new System.Drawing.PointF(20, 12));
                    graphics.DrawString("Hotels | Travels | Shopping | Entertaiment | Home & Business", fontLabel, brush, new System.Drawing.PointF(20, 280));
                    // Añade más campos de texto de la misma manera

                }




                image.Save(memoryStream, ImageFormat.Png);
                return File(memoryStream.ToArray(), "image/png");
            }
        }

        public async Task<IActionResult> CreateAAAReverso(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestLicenceSportInternational.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/AAA", "AAA.jpg");
            var fotoRecienteUrl = requestLicence.PhotoFullPath;


            var pathLogo1 = Path.Combine(_webHostEnvironment.WebRootPath, "images/AAA", "660x74_AAA_discounts-&-rewards-UN_0.png");

            using (var logo1 = System.Drawing.Image.FromFile(pathLogo1))
            using (HttpClient client = new HttpClient())
            using (Stream fotoStream = await client.GetStreamAsync(fotoRecienteUrl))
            using (var fondo = System.Drawing.Image.FromFile(fondoImage))
            using (var foto = System.Drawing.Image.FromStream(fotoStream))
            using (var image = new Bitmap(fondo.Width, fondo.Height))
            using (var graphics = Graphics.FromImage(image))
            using (var memoryStream = new MemoryStream())
            {
                graphics.DrawImage(fondo, 0, 0); // Dibuja el fondo primero
                //graphics.DrawImage(logo1, 30, 75, 450, 80);



                // Utiliza diferentes fuentes y tamaños según la necesidad
                using (var fontTitle = new System.Drawing.Font("Arial", 20, System.Drawing.FontStyle.Regular))
                using (var fontLabel = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold))
                using (var fontBodySmall = new System.Drawing.Font("Arial", 6, System.Drawing.FontStyle.Regular))
                using (var fontBody = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Regular))
                using (var fontBody2 = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                using (var brush2 = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 2))
                {
                    graphics.DrawString("This card is only valid when presented with an original", fontLabel, brush, new System.Drawing.PointF(50, 75));
                    graphics.DrawString("and current membership card of an international club", fontLabel, brush, new System.Drawing.PointF(50, 93));
                    graphics.DrawString("featuring the AAA Discounts & Rewards, Show Your", fontLabel, brush, new System.Drawing.PointF(50, 111));
                    graphics.DrawString("Card & Save or Show your Card! logo.", fontLabel, brush, new System.Drawing.PointF(50, 130));
                    graphics.DrawString("It may not be used for roadside assistance, maps,", fontLabel, brush, new System.Drawing.PointF(50, 160));
                    graphics.DrawString("TripTiks, TourBooks or any other service provided", fontLabel, brush, new System.Drawing.PointF(50, 180));
                    graphics.DrawString("by AAA.", fontLabel, brush, new System.Drawing.PointF(50, 200));
                    graphics.DrawString("EXPIRE: " + DateTime.Now.AddYears(1).Date.ToString("dd/MM/yyyy"), fontLabel, brush, new System.Drawing.PointF(170, 230));


                    graphics.DrawString("AAA Discounts & Rewards", fontTitle, brush2, new System.Drawing.PointF(20, 12));
                    graphics.DrawString("Hotels | Travels | Shopping | Entertaiment | Home & Business", fontLabel, brush, new System.Drawing.PointF(20, 280));
                    // Añade más campos de texto de la misma manera

                }




                image.Save(memoryStream, ImageFormat.Png);
                return File(memoryStream.ToArray(), "image/png");
            }
        }

        public string GenerateHtmlTable(RequestLicenceSportInterantionalViewModel model, int estado)
        {
            string tableHtml = @"
        <table style='border-collapse: collapse; width: 100%; margin-top: 20px;'>";

            // Agregar filas con los datos del modelo
            tableHtml += AddTableRow("Nombre", model.Name);
            tableHtml += AddTableRow("Número Telefónico", model.PhoneNumber);
            tableHtml += AddTableRow("Correo Electrónico", model.Mail);
            tableHtml += AddTableRow("Estado", GetEstadoHtml(estado)); // Nueva fila para el estado
                                                                       // Agregar más filas según sea necesario

            tableHtml += @"
        </table>";

            return tableHtml;
        }

        public string GenerateHtmlTable(RequestLicenceSportInternational model, int estado)
        {
            string tableHtml = @"
        <table style='border-collapse: collapse; width: 100%; margin-top: 20px;'>";

            // Agregar filas con los datos del modelo
            tableHtml += AddTableRow("Nombre", model.Name);
            tableHtml += AddTableRow("Número Telefónico", model.PhoneNumber);
            tableHtml += AddTableRow("Correo Electrónico", model.Mail);
            tableHtml += AddTableRow("Estado", GetEstadoHtml(estado)); // Nueva fila para el estado
                                                                       // Agregar más filas según sea necesario

            tableHtml += @"
        </table>";

            return tableHtml;
        }

        private string AddTableRow(string fieldName, string value)
        {
            return $@"
            <tr>
                <td style='border: 1px solid #dddddd; text-align: left; padding: 8px;'>{fieldName}</td>
                <td style='border: 1px solid #dddddd; text-align: left; padding: 8px;'>{value}</td>
            </tr>";
        }

        private string GetEstadoHtml(int estado)
        {
            string estadoHtml = "";

            switch (estado)
            {
                case 0: // Pendiente
                    estadoHtml = "<span style='color: #999999;'>En Revisión <i class='fa fa-hourglass-half'></i></span>";
                    break;
                case 1: // Aprobado
                    estadoHtml = "<span style='color: green;'><i class='fa fa-check'></i> Aprobado</span>";
                    break;
                case 2: // Rechazado
                    estadoHtml = "<span style='color: red;'><i class='fa fa-times-circle'></i> Rechazado</span>";
                    break;
                default:
                    estadoHtml = "Desconocido";
                    break;
            }

            return estadoHtml;
        }



        public IActionResult Thanks()
        {
            return View();
        }

        private bool ContieneNumeroIdentificacion(string texto)
        {
            // Patrón de expresión regular para el formato específico del número de identificación
            string patron = @"\b\d\s\d{4}\s\d{4}\b";

            // Se crea una instancia de Regex con el patrón
            Regex regex = new Regex(patron);

            // Se verifica si hay coincidencias en el texto
            Match match = regex.Match(texto);

            return match.Success;
        }

        private bool VencimientoDocument(string texto)
        {
            // Patrón de expresión regular para extraer el año de la fecha de vencimiento
            string patronFechaVencimiento = @"\b(\d{4})\b";

            // Se crea una instancia de Regex con el patrón
            Regex regexFechaVencimiento = new Regex(patronFechaVencimiento);

            // Se buscan todas las coincidencias en el texto
            MatchCollection matchesFechaVencimiento = regexFechaVencimiento.Matches(texto);

            // Verifica si hay al menos dos coincidencias
            if (matchesFechaVencimiento.Count >= 2)
            {
                // Obtiene el año de la segunda fecha de vencimiento
                string anoVencimiento = matchesFechaVencimiento[1].Groups[1].Value;

                // Intenta convertir el año a un valor numérico
                if (int.TryParse(anoVencimiento, out int añoVencimientoNumerico))
                {
                    // Año de comparación
                    // Sacar el presente año + 1 
                    DateTime dateTime = DateTime.Now;

                    int añoComparacion = dateTime.Year + 1;

                    // Comparación de años (ahora, se verifica si el año de vencimiento es mayor al año de comparación)
                    if (añoVencimientoNumerico > añoComparacion)
                    {
                        Console.WriteLine("El año de vencimiento es posterior al año de comparación.");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("El año de vencimiento no es mayor al año de comparación. ¡Error!");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("No se pudo convertir el año de vencimiento a un valor numérico.");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("No se encontraron al menos dos fechas de vencimiento en el texto.");
                return false;
            }
        }




        private async Task<string> GuardarArchivoEnServidor(IFormFile file, string nombreArchivo)
        {
            // Lógica para guardar el archivo en el servidor y devolver la ruta
            // Puedes usar el nombre original del archivo o generar uno único

            var rutaArchivo = Path.Combine(_webHostEnvironment.WebRootPath, nombreArchivo);


            using (var stream = new FileStream(rutaArchivo, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return rutaArchivo;
        }

        public async Task<string> EjemploLecturaTextoEnImagenAsync(string pdfPath, string guid)
        {

            // Lee el texto en la imagen
            var textoLeido = await LeerTextoEnImagenAsync(pdfPath, guid);



            // Imprime el texto leído
            Console.WriteLine($"Texto leído en la imagen:\n{textoLeido}");

            return textoLeido;
        }

        private async Task<string> LeerTextoEnImagenAsync(string pdfName, string guid)
        {
            try
            {

                var pdfPath = pdfName;
                var tesseractPath = Path.Combine(_webHostEnvironment.WebRootPath, "spa.traineddata");


                var pdf = PdfDocument.FromFile(pdfPath);

                var rasterize = Path.Combine(_webHostEnvironment.WebRootPath, @"files\images\" + guid + "_*.png");



                // Extract all pages to a folder as image files
                pdf.RasterizeToImageFiles(rasterize);

                // Dimensions and page ranges may be specified
                //pdf.RasterizeToImageFiles(@"C:\image\folder\example_pdf_image_*.jpg", 100, 80);

                // Realizar OCR en cada imagen generada
                string docSTR = string.Empty;

                for (int i = 0; i < pdf.PageCount; i++)
                {
                    string imagePathResult = Path.Combine(_webHostEnvironment.WebRootPath, @"files\images\" + guid + "_" + (i + 1) + ".png");
                    string textoExtraido = RealizarOCR(imagePathResult);
                    docSTR += textoExtraido;
                    // Imprimir el texto extraído
                    Console.WriteLine($"Texto extraído de la página {i + 1}: {textoExtraido}");
                }

                // Extract all pages as AnyBitmap objects
                AnyBitmap[] pdfBitmaps = pdf.ToBitmap();


                if (docSTR.Contains("EXAMEN MEDICO DE APTITUD\r\nPARA LA OBTENCION DE\r\nLA LICENCIA DEPORTIVA"))
                {
                    Console.WriteLine("Se encuentra");
                }

                return docSTR;

            }
            catch (Exception ex)
            {
                // Maneja las excepciones según tus necesidades
                Console.WriteLine($"Error al leer texto en la imagen: {ex.Message}");
                return string.Empty;
            }


        }

        private static string RealizarOCR(string imagePath)
        {
            // Configurar la licencia de IronOCR
            IronOcr.License.LicenseKey = "IRONSUITE.DONOVANJARQUIN1.GMAIL.COM.30767-9329948699-C6Q6D5R-4NKHW4DNCWEE-3T6TOY6MFDBX-Y5MTPURA3747-FP53PY4QDSPI-RX5YNDELRTHZ-L6PGWHEQSUWJ-GUEDE3-TFWTZ2653X6LUA-DEPLOYMENT.TRIAL-SVGYVQ.TRIAL.EXPIRES.28.FEB.2024";

            var Ocr = new IronTesseract();
            using (var input = new OcrInput(imagePath))
            {
                input.EnhanceResolution();
                input.DeNoise();

                var result = Ocr.Read(input);

                return result.Text;
            }
        }

        private bool ContienePalabra(string texto, string palabra)
        {
            // Puedes implementar tu lógica de búsqueda aquí, por ejemplo, utilizando expresiones regulares.
            return texto.Contains(palabra, StringComparison.OrdinalIgnoreCase);
        }


        public async Task<IActionResult> UpdateFullApproved(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSportInternational = await _context.RequestLicenceSportInternational.FindAsync(id);
            if (requestLicenceSportInternational == null)
            {
                return NotFound();
            }
            
            requestLicenceSportInternational.FullApproved = !requestLicenceSportInternational.FullApproved;

            if (requestLicenceSportInternational.FullApproved)
            {
                requestLicenceSportInternational.Approved = DateTime.Now;
                requestLicenceSportInternational.Rejection = null;
                requestLicenceSportInternational.FullRejection = false;


                _mailHelper.SendMail(requestLicenceSportInternational.Mail, "Aprobación de Solicitud de permiso internacional", 
                    _configurationApp.MessagePermisoApproved + GenerateHtmlTable(requestLicenceSportInternational, 1));

                _mailHelper.SendMail(_configuration["Mail:Admin"], "Aprobación de Solicitud de permiso internacional",
                    _configurationApp.MessagePermisoAdminAprobado + GenerateHtmlTable(requestLicenceSportInternational, 1));
            }
            else 
            {
                requestLicenceSportInternational.Approved= null;
            }

            requestLicenceSportInternational.Modify = DateTime.Now;

            _context.Update(requestLicenceSportInternational);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UpdateFullRejection(int? id, string? motive)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSportInternational = await _context.RequestLicenceSportInternational.FindAsync(id);
            if (requestLicenceSportInternational == null)
            {
                return NotFound();
            }

            requestLicenceSportInternational.FullRejection = !requestLicenceSportInternational.FullRejection;

            if (requestLicenceSportInternational.FullRejection)
            {
                requestLicenceSportInternational.Rejection = DateTime.Now;
                requestLicenceSportInternational.Approved = null;
                requestLicenceSportInternational.FullApproved= false;

                _context.Add(new Rejection()
                {
                    Create = DateTime.Now,
                    Motive = motive,
                    RequestLicenceSportInternationalId = requestLicenceSportInternational.RequestLicenceSportInternationalId
                });

                await _context.SaveChangesAsync();

                _mailHelper.SendMail(requestLicenceSportInternational.Mail, "Rechazo de Solicitud de permiso internacional", 
                    _configurationApp.MessagePermisoRejection + GenerateHtmlTable(requestLicenceSportInternational, 2) + "<br /> <b>Motivo:</b> <br/>" + motive);

                _mailHelper.SendMail(_configuration["Mail:Admin"], "Rechazo de Solicitud de permiso internacional",
                    _configurationApp.MessagePermisoAdminRejection +  GenerateHtmlTable(requestLicenceSportInternational, 2) + "<br /> <b>Motivo:</b> <br/>" + motive);
            }
            else
            {
                requestLicenceSportInternational.Rejection = null;
            }

            requestLicenceSportInternational.Modify = DateTime.Now;

            _context.Update(requestLicenceSportInternational);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: RequestLicenceSportInternationals/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSportInternational = await _context.RequestLicenceSportInternational.FindAsync(id);
            if (requestLicenceSportInternational == null)
            {
                return NotFound();
            }
            ViewData["DomicileCountryId"] = new SelectList(_context.Countries, "CountryId", "CountryId", requestLicenceSportInternational.DomicileCountryId);
            ViewData["ResidenceCountryId"] = new SelectList(_context.Countries, "CountryId", "CountryId", requestLicenceSportInternational.ResidenceCountryId);
            return View(requestLicenceSportInternational);
        }

        // POST: RequestLicenceSportInternationals/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RequestLicenceSportInternationalId,Name,Mail,PhoneNumber,Address,Travel,IdentificationFP,IdentificationTP,LicenceFP,LicenceTP,Photo,ResidenceCountryId,DomicileCountryId,Create,Modify,Approved,FullApproved")] RequestLicenceSportInternational requestLicenceSportInternational)
        {
            if (id != requestLicenceSportInternational.RequestLicenceSportInternationalId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(requestLicenceSportInternational);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RequestLicenceSportInternationalExists(requestLicenceSportInternational.RequestLicenceSportInternationalId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DomicileCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", requestLicenceSportInternational.DomicileCountryId);
            ViewData["ResidenceCountryId"] = new SelectList(_context.Countries, "CountryId", "Name", requestLicenceSportInternational.ResidenceCountryId);
            return View(requestLicenceSportInternational);
        }

        // GET: RequestLicenceSportInternationals/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSportInternational = await _context.RequestLicenceSportInternational
                .Include(r => r.DomicileCountry)
                .Include(r => r.ResidenceCountry)
                .FirstOrDefaultAsync(m => m.RequestLicenceSportInternationalId == id);
            if (requestLicenceSportInternational == null)
            {
                return NotFound();
            }

            return View(requestLicenceSportInternational);
        }

        // POST: RequestLicenceSportInternationals/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var requestLicenceSportInternational = await _context.RequestLicenceSportInternational.FindAsync(id);
            if (requestLicenceSportInternational != null)
            {
                _context.RequestLicenceSportInternational.Remove(requestLicenceSportInternational);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RequestLicenceSportInternationalExists(int id)
        {
            return _context.RequestLicenceSportInternational.Any(e => e.RequestLicenceSportInternationalId == id);
        }
    }
}
