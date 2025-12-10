using AutomovilClub.Backend.Data;
using AutomovilClub.Backend.Data.Entities;
using AutomovilClub.Backend.Enums;
using AutomovilClub.Backend.Helpers;
using AutomovilClub.Backend.Models;
using IronOcr;
using IronSoftware.Drawing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1;
using QRCoder;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using static NuGet.Packaging.PackagingConstants;



namespace AutomovilClub.Backend.Controllers
{
    public class RequestAssociateMembershipsController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMailHelper _mailHelper;
        private readonly IConfiguration _configuration;
        private readonly IFontsHelper _fontsHelper;
        private readonly Data.Entities.Configuration _configurationApp;

        public RequestAssociateMembershipsController(DataContext context, 
            IWebHostEnvironment webHostEnvironment,
            IMailHelper mailHelper,
            IConfiguration configuration,
            IFontsHelper fontsHelper)
        {
            _webHostEnvironment = webHostEnvironment;
            _mailHelper = mailHelper;
            _context = context;
            _configuration = configuration;
            _fontsHelper = fontsHelper;
            var configurations= _context.Configurations.ToList();
            if (configurations.Count > 0)
            {
                _configurationApp = configurations[0];
            }
        }

        public async Task<IActionResult> Index(int? filterTypeId = 0, string? userFilterId = "-1")
        {
            var dataContext =await _context.RequestAssociateMemberships.Include(u => u.User).ToListAsync();

            if (userFilterId != null && userFilterId != "-1")
            {
                dataContext = dataContext
                                .Where(r => r.UserId == userFilterId).ToList();
            }

            ViewBag.FilterTypeId = filterTypeId;

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

            var requestLicenceSport = await _context.RequestAssociateMemberships.FindAsync(id);
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
                RequestId = requestLicenceSport.RequestAssociateMembershipId,
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
                    var request = await _context.RequestAssociateMemberships.FindAsync(model.RequestId);

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


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSport = await _context.RequestLicenceSports
                .Include(r => r.Country)
                .FirstOrDefaultAsync(m => m.RequestLicenceSportId == id);
            if (requestLicenceSport == null)
            {
                return NotFound();
            }

            return View(requestLicenceSport);
        }

        public IActionResult Thanks()
        {
            return View();
        }

        public async Task<IActionResult> FindPerson(string identification)
        {
            var person = _context.People.Where(p => p.Identification == identification).FirstOrDefault();

            if (person == null)
            {
                return Ok(new { succes = false, message = "No existe" });
            }

            return Ok(new { succes = true, data = person });
        }

        public IActionResult Create()
        {
            return View(new RequestAssociateMembership());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RequestAssociateMembership model)
        {
            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();

                _mailHelper.SendMail(model.Mail, "Solicitud de Membresía para Asociado",
                    _configurationApp.MessageMembresia + GenerateHtmlTable(model, 0));

                _mailHelper.SendMail(_configuration["Mail:Admin"], "Solicitud de Membresía para Asociado", 
                    _configurationApp.MessageMembresiaAdmin + GenerateHtmlTable(model, 0));

                return RedirectToAction(nameof(Thanks));
            }
            return View(model);
        }

        public string GenerateHtmlTable(RequestAssociateMembership model, int estado)
        {
            string tableHtml = @"
        <table style='border-collapse: collapse; width: 100%; margin-top: 20px;'>";

            // Agregar filas con los datos del modelo
            tableHtml += AddTableRow("Identificación", model.Identification);
            tableHtml += AddTableRow("Nombre", model.FullName);
            tableHtml += AddTableRow("Email", model.Mail);
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


        //Funcion para crear una imagen descargable con la Membresia del asociado
        //Debe ser una imagne con fondo naranja con los datos del asociado
        // y ademas debe llevar el logo de automovil club
        //public async Task<IActionResult> CreateImageAnverso(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var requestAssociateMembership = await _context.RequestAssociateMemberships.FindAsync(id);
        //    if (requestAssociateMembership == null)
        //    {
        //        return NotFound();
        //    }

        //    var pathLogo1 = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Logo2.png");
        //    var pathLogo2 = Path.Combine(_webHostEnvironment.WebRootPath, "images", "fia-logo.png");
        //    using (var logo1 = System.Drawing.Image.FromFile(pathLogo1))
        //    using (var logo2 = System.Drawing.Image.FromFile(pathLogo2))
        //    using (var image = new Bitmap(600, 800)) // Hacer la imagen más angosta
        //    using (var graphics = Graphics.FromImage(image))
        //    using (var memoryStream = new MemoryStream())
        //    {
        //        graphics.Clear(System.Drawing.Color.Orange);
        //        graphics.DrawImage(logo1, 100, 50, 400, 225); // El logo es ahora el doble de grande y sigue estando centrado


        //        using (var font = new System.Drawing.Font("Consolas", 48, System.Drawing.FontStyle.Bold))
        //        using (var brush = new SolidBrush(System.Drawing.Color.Black))
        //        {
        //            graphics.DrawString("LICENCIA", font, brush, new System.Drawing.PointF(150, 310)); // Centrado y con margen arriba
        //            graphics.DrawString("NACIONAL", font, brush, new System.Drawing.PointF(150, 380)); // Centrado y con margen arriba
        //        }

        //        // Dibuja el segundo logo casi al final
        //        graphics.DrawImage(logo2, 200, 500, 200, 200); // Centrado y con margen abajo

        //        // Dibuja el texto "Final de la imagen" al lado del logo de FIA
        //        using (var font = new System.Drawing.Font("Arial", 12))
        //        using (var brush = new SolidBrush(System.Drawing.Color.Black))
        //        {
        //            graphics.DrawString("Fèdèration Internationale de l' Automobile", font, brush, new System.Drawing.PointF(150, 740)); // Centrado y con margen arriba
        //        }


        //        image.Save(memoryStream, ImageFormat.Png);
        //        return File(memoryStream.ToArray(), "image/png");
        //    }
        //}


        //public async Task<IActionResult> CreateImageAnverso2(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var requestLicence = await _context.RequestAssociateMemberships.FindAsync(id);
        //    if (requestLicence == null)
        //    {
        //        return NotFound();
        //    }

        //    var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/MEMBRESIA", "MEMBRESIAfondo1.jpg");

        //    if (requestLicence.Expiration != null)
        //    {
        //        try
        //        {
        //            using (var client = new HttpClient())
        //            using (var fondo = System.Drawing.Image.FromFile(fondoImage))
        //            using (var image = new Bitmap(fondo.Width, fondo.Height))
        //            using (var graphics = Graphics.FromImage(image))
        //            using (var memoryStream = new MemoryStream())
        //            {
        //                graphics.DrawImage(fondo, 0, 0);

        //                using (var fontTitle = _fontsHelper.GetFont("OPTIEdgar-Extended", 17, System.Drawing.FontStyle.Bold))
        //                using (var fontTitle2 = _fontsHelper.GetFont("OPTIEdgar-Extended", 12, System.Drawing.FontStyle.Regular))
        //                using (var fontTitle3 = _fontsHelper.GetFont("OPTIEdgar-Extended", 8, System.Drawing.FontStyle.Regular))
        //                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
        //                using (var brush2 = new System.Drawing.SolidBrush(System.Drawing.Color.White))
        //                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 2))
        //                {
        //                    // Calculate right alignment positions
        //                    var rightEdge = fondo.Width - 10; // 10 pixels padding from the right edge

        //                    // Draw the ShortName
        //                    var shortNameSize = graphics.MeasureString(requestLicence.ShortName, fontTitle);
        //                    var shortNamePosition = new System.Drawing.PointF(rightEdge - shortNameSize.Width + 4, 35);
        //                    graphics.DrawString(requestLicence.ShortName, fontTitle, brush2, shortNamePosition);

        //                    // Draw the AssociateType
        //                    var associateType = requestLicence.AssociateType.ToString().ToUpper().Replace("_", " ");
        //                    var associateTypeSize = graphics.MeasureString(associateType, fontTitle3);
        //                    var associateTypePosition = new System.Drawing.PointF(rightEdge - associateTypeSize.Width -4, 85);
        //                    graphics.DrawString(associateType, fontTitle3, brush2, associateTypePosition);

        //                    // Draw the Number
        //                    var numberSize = graphics.MeasureString(requestLicence.Identification, fontTitle2);
        //                    var numberPosition = new System.Drawing.PointF(rightEdge - numberSize.Width, 62);
        //                    graphics.DrawString(requestLicence.Identification, fontTitle2, brush2, numberPosition);

        //                    // Draw the Expiration date
        //                    var expirationDate = requestLicence.Expiration.Value.ToString("d/M/yyyy");
        //                    var expirationDateSize = graphics.MeasureString(expirationDate, fontTitle3);
        //                    var expirationDatePosition = new System.Drawing.PointF(15, 295);
        //                    graphics.DrawString(expirationDate, fontTitle3, brush2, expirationDatePosition);
        //                }

        //                image.Save(memoryStream, ImageFormat.Png);
        //                return File(memoryStream.ToArray(), "image/png");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            // Log the exception
        //            Console.Error.WriteLine($"An error occurred: {ex.Message}");
        //            return StatusCode(500, "Internal server error");
        //        }
        //    }

        //    return RedirectToAction(nameof(Index));
        //}


        //public async Task<IActionResult> CreateImageAnverso2(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var requestLicence = await _context.RequestAssociateMemberships.FindAsync(id);
        //    if (requestLicence == null)
        //    {
        //        return NotFound();
        //    }

        //    var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/MEMBRESIA", "MEMBRESIAfondo1.jpg");

        //    if (requestLicence.Expiration != null)
        //    {
        //        try
        //        {
        //            using (var client = new HttpClient())
        //            using (var fondo = System.Drawing.Image.FromFile(fondoImage))
        //            using (var image = new Bitmap(fondo.Width, fondo.Height))
        //            using (var graphics = Graphics.FromImage(image))
        //            using (var memoryStream = new MemoryStream())
        //            {
        //                graphics.DrawImage(fondo, 0, 0);

        //                // Font and brush setup
        //                using (var fontTitle = _fontsHelper.GetFont("OPTIEdgar-Extended", 17, System.Drawing.FontStyle.Bold))
        //                using (var fontTitle2 = _fontsHelper.GetFont("OPTIEdgar-Extended", 12, System.Drawing.FontStyle.Regular))
        //                using (var fontTitle3 = _fontsHelper.GetFont("OPTIEdgar-Extended", 8, System.Drawing.FontStyle.Regular))
        //                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
        //                using (var brush2 = new System.Drawing.SolidBrush(System.Drawing.Color.White))
        //                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 2))
        //                {
        //                    // Constants
        //                    int logoMargin = 150;  // Assume the logo occupies the first 100px on the left
        //                    int padding = 10;
        //                    int availableWidth = fondo.Width - logoMargin - padding;

        //                    // Right alignment position (10 pixels padding from the right edge)
        //                    var rightEdge = fondo.Width - 10;

        //                    // Split and draw ShortName if it exceeds available width
        //                    var (firstLine, secondLine) = SplitTextIfTooLong(requestLicence.ShortName, availableWidth, graphics, fontTitle);

        //                    // Draw the first line of ShortName on the left (considering logoMargin)
        //                    var shortNameSize = graphics.MeasureString(firstLine, fontTitle);
        //                    var shortNamePosition = new System.Drawing.PointF(logoMargin, 35);
        //                    graphics.DrawString(firstLine, fontTitle, brush2, shortNamePosition);

        //                    // Draw the second line if it exists
        //                    if (!string.IsNullOrEmpty(secondLine))
        //                    {
        //                        var secondLineSize = graphics.MeasureString(secondLine, fontTitle);
        //                        var secondLinePosition = new System.Drawing.PointF(rightEdge  - secondLineSize.Width, 35 + shortNameSize.Height);
        //                        graphics.DrawString(secondLine, fontTitle, brush2, secondLinePosition);
        //                    }

        //                    // Calculate the vertical shift based on how many lines were drawn
        //                    float nextYPosition = 35 + shortNameSize.Height;
        //                    if (!string.IsNullOrEmpty(secondLine))
        //                    {
        //                        nextYPosition += shortNameSize.Height + 5;
        //                    }

        //                    // Right-aligned text starts here

        //                    //// Draw the Number on the right, aligned with `rightEdge`
        //                    //var numberSize = graphics.MeasureString(requestLicence.Identification, fontTitle2);
        //                    //var numberPosition = new System.Drawing.PointF(rightEdge - numberSize.Width, 62);
        //                    //graphics.DrawString(requestLicence.Identification, fontTitle2, brush2, numberPosition);

        //                    //// Draw the AssociateType on the right, aligned with `rightEdge`
        //                    //var associateType = requestLicence.AssociateType.ToString().ToUpper().Replace("_", " ");
        //                    //var associateTypeSize = graphics.MeasureString(associateType, fontTitle3);
        //                    //var associateTypePosition = new System.Drawing.PointF(rightEdge - associateTypeSize.Width, 85);
        //                    //graphics.DrawString(associateType, fontTitle3, brush2, associateTypePosition);

        //                    // Draw the Expiration date on the left (below the wrapped ShortName)
        //                    var expirationDate = requestLicence.Expiration.Value.ToString("d/M/yyyy");
        //                    var expirationDateSize = graphics.MeasureString(expirationDate, fontTitle3);
        //                    var expirationDatePosition = new System.Drawing.PointF(15, 295);
        //                    graphics.DrawString(expirationDate, fontTitle3, brush2, expirationDatePosition);
        //                }

        //                image.Save(memoryStream, ImageFormat.Png);
        //                return File(memoryStream.ToArray(), "image/png");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            // Log the exception
        //            Console.Error.WriteLine($"An error occurred: {ex.Message}");
        //            return StatusCode(500, "Internal server error");
        //        }
        //    }

        //    return RedirectToAction(nameof(Index));
        //}

        public async Task<IActionResult> CreateImageAnverso2(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestAssociateMemberships.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }

            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/MEMBRESIA", "Membresia_01.png");

            if (requestLicence.Expiration != null)
            {
                try
                {
                    using (var client = new HttpClient())
                    using (var fondo = System.Drawing.Image.FromFile(fondoImage))
                    using (var image = new Bitmap(fondo.Width, fondo.Height))
                    using (var graphics = Graphics.FromImage(image))
                    using (var memoryStream = new MemoryStream())
                    {
                        graphics.DrawImage(fondo, 0, 0, 2092, 1330);

                        // Font and brush setup
                        using (var initialFont = _fontsHelper.GetFont("OPTIEdgar-Extended", 62, System.Drawing.FontStyle.Bold))
                        using (var fontTitle2 = _fontsHelper.GetFont("OPTIEdgar-Extended", 38, System.Drawing.FontStyle.Regular))
                        using (var fontTitle3 = _fontsHelper.GetFont("OPTIEdgar-Extended", 34, System.Drawing.FontStyle.Regular))
                        using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                        using (var brush2 = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 2))
                        {
                            // Constants
                            int logoMargin = 124;  // Assume the logo occupies the first 150px on the left
                            int padding = 10;
                            int availableWidth = fondo.Width - logoMargin - padding;

                            // Right alignment position (10 pixels padding from the right edge)
                            var rightEdge = 77;

                            // Dynamically reduce font size if text exceeds the available width
                            var fontTitle = GetFittingFont(graphics, requestLicence.ShortName, initialFont, availableWidth);
                            

                            // Draw the ShortName on the left (considering logoMargin)
                            var shortNameSize = graphics.MeasureString(requestLicence.ShortName, fontTitle);
                            var shortNamePosition = new System.Drawing.PointF(rightEdge, 950);
                            graphics.DrawString(requestLicence.ShortName, fontTitle, brush2, shortNamePosition);

                             //Draw the AssociateType
                            //var associateType = requestLicence.AssociateType.ToString().ToUpper().Replace("_", " ");
                            //var associateTypeSize = graphics.MeasureString(associateType, fontTitle3);
                            //var associateTypePosition = new System.Drawing.PointF(rightEdge, 85);
                            //graphics.DrawString(associateType, fontTitle3, brush2, associateTypePosition);

                            // Draw the Number
                            var numberSize = graphics.MeasureString(requestLicence.Identification, fontTitle2);
                            var numberPosition = new System.Drawing.PointF(rightEdge, 1075);
                            graphics.DrawString(requestLicence.Identification, fontTitle2, brush2, numberPosition);

                            // Draw the Expiration date
                            var expirationDate = requestLicence.Expiration.Value.ToString("d/M/yyyy");
                            var expirationDateSize = graphics.MeasureString(expirationDate, fontTitle3);
                            var expirationDatePosition = new System.Drawing.PointF(300, 1170);
                            graphics.DrawString(expirationDate, fontTitle3, brush2, expirationDatePosition);
                        }

                        image.Save(memoryStream, ImageFormat.Png);
                        return File(memoryStream.ToArray(), "image/png");
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Console.Error.WriteLine($"An error occurred: {ex.Message}");
                    return StatusCode(500, "Internal server error");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private System.Drawing.Font GetFittingFont(Graphics graphics, string text, System.Drawing.Font originalFont, int maxWidth)
        {
            // Start with the original font size
            float fontSize = originalFont.Size;

            // Keep reducing the font size until the text fits within the available width
            while (fontSize > 8) // Set a minimum font size to avoid making it too small
            {
                using (var font = new System.Drawing.Font(originalFont.FontFamily, fontSize, originalFont.Style))
                {
                    var textSize = graphics.MeasureString(text, font);
                    if (textSize.Width <= maxWidth)
                    {
                        return new System.Drawing.Font(originalFont.FontFamily, fontSize, originalFont.Style);
                    }
                }
                fontSize -= 0.5f; // Reduce the font size step by step
            }

            // Return the smallest possible font if nothing fits
            return new System.Drawing.Font(originalFont.FontFamily, 8, originalFont.Style);
        }

        private (string firstLine, string secondLine) SplitTextIfTooLong(string text, int maxWidth, Graphics graphics, System.Drawing.Font font)
        {
            // Measure the width of the entire text
            var textSize = graphics.MeasureString(text, font);

            // If the text fits within the available width, return it as a single line
            if (textSize.Width <= maxWidth)
            {
                return (text, string.Empty);
            }

            // Otherwise, find the point to split the text into two lines
            for (int i = text.Length - 1; i >= 0; i--)
            {
                string candidateFirstLine = text.Substring(0, i);
                string candidateSecondLine = text.Substring(i);

                // Measure if the first part fits
                var candidateSize = graphics.MeasureString(candidateFirstLine, font);
                if (candidateSize.Width <= maxWidth)
                {
                    return (candidateFirstLine, candidateSecondLine.Trim());
                }
            }

            // If we couldn't find a good split, just return the original text (as fallback)
            return (text, string.Empty);
        }


        public async Task<IActionResult> EditPartial(int? id, int? filterTypeId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSport = await _context.RequestAssociateMemberships.FindAsync(id);
            if (requestLicenceSport == null)
            {
                return NotFound();
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


            var model = new RequestAssociateMembershipsViewModelShort()
            {
                Expedition = requestLicenceSport.Expedition,
                Expiration = requestLicenceSport.Expiration,
                AssociateType  = requestLicenceSport.AssociateType,
                RequestAssociateMembershipId = requestLicenceSport.RequestAssociateMembershipId,
                ShortName = requestLicenceSport.ShortName,
                Number= requestLicenceSport.Identification,
                FilterTypeId = filterTypeId,
            };

            if (model.ShortName == null)
                model.ShortName = requestLicenceSport.FullName;

            return View("_EditPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPartial(RequestAssociateMembershipsViewModelShort model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var requestLicenceSport = await _context.RequestAssociateMemberships.FindAsync(model.RequestAssociateMembershipId);

                    requestLicenceSport.Expedition = model.Expedition;
                    requestLicenceSport.Expiration = model.Expiration;
                    requestLicenceSport.AssociateType = model.AssociateType;
                    requestLicenceSport.ShortName = model.ShortName;
                    requestLicenceSport.Identification = model.Number;

                    _context.Update(requestLicenceSport);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RequestLicenceSportExists(model.RequestAssociateMembershipId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { model.FilterTypeId });
            }

            return View(model);
        }

        public async Task<IActionResult> CreateImageReverso2(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestAssociateMemberships.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/MEMBRESIA", "Membresia_03.png");
            //var fotoRecienteUrl = requestLicence.PhotoFullPath;


            using (HttpClient client = new HttpClient())
            //using (Stream fotoStream = await client.GetStreamAsync(fotoRecienteUrl))
            using (var fondo = System.Drawing.Image.FromFile(fondoImage))
            //using (var foto = System.Drawing.Image.FromStream(fotoStream))
            using (var image = new Bitmap(fondo.Width, fondo.Height))
            using (var graphics = Graphics.FromImage(image))
            using (var memoryStream = new MemoryStream())
            {
                graphics.DrawImage(fondo, 0, 0, 2092, 1330); // Dibuja el fondo primero

                // Genera el código QR con los datos deseados
                //string datosCompletos = $"{requestLicence.FullName}\nFecha de nacimiento:\nFecha de expiración: {DateTime.Now.AddYears(1).ToString("d/M/yyyy")}";

                //QRCodeGenerator qrGenerator = new QRCodeGenerator();
                //QRCodeData qrCodeData = qrGenerator.CreateQrCode(datosCompletos, QRCodeGenerator.ECCLevel.Q);
                //var qrCode = new PngByteQRCode(qrCodeData);
                //byte[] qrCodeImageBytes = qrCode.GetGraphic(20);

                //// Convertir el array de bytes a un objeto Image
                //using (MemoryStream ms = new MemoryStream(qrCodeImageBytes))
                //{
                //    System.Drawing.Image qrImage = System.Drawing.Image.FromStream(ms);

                //    // Define el tamaño deseado del código QR (por ejemplo, 100x100)
                //    int qrWidth = 500;
                //    int qrHeight = 500;

                //    // Define la posición donde se dibujará el código QR en la imagen principal
                //    int qrX = 85; // Posición en el eje X
                //    int qrY = 195; // Posición en el eje Y

                //    // Dibujar el código QR en la imagen principal
                //    graphics.DrawImage(qrImage, new System.Drawing.Rectangle(qrX, qrY, qrWidth, qrHeight));
                //}
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

            var requestLicence = await _context.RequestAssociateMemberships.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/AAA", "AAA.jpg");
            //var fotoRecienteUrl = requestLicence.PhotoFullPath;


            var pathLogo1 = Path.Combine(_webHostEnvironment.WebRootPath, "images/AAA", "660x74_AAA_discounts-&-rewards-UN_0.png");

            using (var logo1 = System.Drawing.Image.FromFile(pathLogo1))
            using (HttpClient client = new HttpClient())
            //using (Stream fotoStream = await client.GetStreamAsync(fotoRecienteUrl))
            using (var fondo = System.Drawing.Image.FromFile(fondoImage))
            //using (var foto = System.Drawing.Image.FromStream(fotoStream))
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

            var requestLicence = await _context.RequestAssociateMemberships.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/AAA", "AAA.jpg");
            


            var pathLogo1 = Path.Combine(_webHostEnvironment.WebRootPath, "images/AAA", "660x74_AAA_discounts-&-rewards-UN_0.png");


            if (requestLicence.Expiration!=null)
            {
                using (var logo1 = System.Drawing.Image.FromFile(pathLogo1))
                using (HttpClient client = new HttpClient())
                using (var fondo = System.Drawing.Image.FromFile(fondoImage))
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
                        graphics.DrawString("EXPIRE: " + requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontLabel, brush, new System.Drawing.PointF(170, 230));


                        graphics.DrawString("AAA Discounts & Rewards", fontTitle, brush2, new System.Drawing.PointF(20, 12));
                        graphics.DrawString("Hotels | Travels | Shopping | Entertaiment | Home & Business", fontLabel, brush, new System.Drawing.PointF(20, 280));
                        // Añade más campos de texto de la misma manera

                    }




                    image.Save(memoryStream, ImageFormat.Png);
                    return File(memoryStream.ToArray(), "image/png");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        //Funcion para crear una imagen descargable con la Membresia del asociado
        //Debe ser una imagne con fondo naranja con los datos del asociado
        // y ademas debe llevar el logo de automovil club
        //public async Task<IActionResult> CreateImageReverso(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var requestAssociateMembership = await _context.RequestAssociateMemberships.FindAsync(id);
        //    if (requestAssociateMembership == null)
        //    {
        //        return NotFound();
        //    }


        //    using (var image = new Bitmap(600, 800)) // Hacer la imagen más angosta
        //    using (var graphics = Graphics.FromImage(image))
        //    using (var memoryStream = new MemoryStream())
        //    {
        //        graphics.Clear(System.Drawing.Color.Orange);

        //        using (var font = new System.Drawing.Font("Consolas", 34, System.Drawing.FontStyle.Bold))
        //        using (var brush = new SolidBrush(System.Drawing.Color.Black))
        //        {
        //            graphics.DrawString(requestAssociateMembership.Identification, font, brush, new System.Drawing.PointF(175, 50)); // Centrado y con margen arriba
        //            graphics.DrawString(requestAssociateMembership.FullName, font, brush, new System.Drawing.PointF(20, 120)); // Centrado y con margen arriba
        //        }

        //        using (var font = new System.Drawing.Font("Arial", 12))
        //        using (var brush = new SolidBrush(System.Drawing.Color.Black))
        //        {
        //            graphics.DrawString("N. Licencia:", font, brush, new System.Drawing.PointF(50, 300)); // Centrado y con margen arriba
        //            graphics.DrawString("Grado:", font, brush, new System.Drawing.PointF(50, 350)); // Centrado y con margen arriba
        //            graphics.DrawString("Fecha de Nacimiento:", font, brush, new System.Drawing.PointF(50, 400)); // Centrado y con margen arriba
        //            graphics.DrawString("Expedición:", font, brush, new System.Drawing.PointF(50, 450)); // Centrado y con margen arriba
        //            graphics.DrawString("Vencimiento:", font, brush, new System.Drawing.PointF(50, 500)); // Centrado y con margen arriba
        //            graphics.DrawString("Vista corregida (gafas o lentes):", font, brush, new System.Drawing.PointF(50, 550)); // Centrado y con margen arriba
        //            graphics.DrawString("Supervisión médica especial:", font, brush, new System.Drawing.PointF(50, 600)); // Centrado y con margen arriba
        //            graphics.DrawString("Consentimiento para el procesamiento de datos personales en la WADB:", font, brush, new System.Drawing.PointF(50, 650)); // Centrado y con margen arriba
        //        }


        //        image.Save(memoryStream, ImageFormat.Png);
        //        return File(memoryStream.ToArray(), "image/png");
        //    }
        //}


        //public async Task<string> UploadPhoto(IFormFile file, string folder)
        //{
        //    string path = string.Empty;
        //    string pic = string.Empty;

        //    if (file != null)
        //    {
        //        pic = Path.GetFileName(file.FileName);
        //        string raiz = _env.WebRootPath.ToString();

        //        path = $"{raiz}{folder}{pic}";

        //        using (Stream fileStream = new FileStream(path, FileMode.Create))
        //        {
        //            await file.CopyToAsync(fileStream);
        //        }
        //    }

        //    return pic;
        //}

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

        // Métodos auxiliares

        //private async Task<string> LeerContenidoPDFAsync(IFormFile file)
        //{
        //    using (var memoryStream = new MemoryStream())
        //    {
        //        await file.CopyToAsync(memoryStream);
        //        memoryStream.Position = 0;

        //        //using (var pdfDocument = PdfReader.Open(memoryStream))
        //        //{
        //        //    StringWriter output = new StringWriter();

        //        //    for (int i = 0; i < pdfDocument.Pages.Count; i++)
        //        //    {
        //        //        output.WriteLine(ExtraerTextoOCR(ConvertPdfPageToImage(pdfDocument.Pages[i])));
        //        //    }

        //        //    return output.ToString();
        //        //}


        //        //using (var pdfRenderer = new PdfRenderer(document))
        //        //{
        //        //    // Supongamos que quieres renderizar la primera página del PDF
        //        //    var pageIndex = 0;
        //        //    var image = pdfRenderer.Render(pageIndex);

        //        //    using (var memoryStream = new MemoryStream())
        //        //    {
        //        //        // Convierte la imagen a formato PNG y guarda en el MemoryStream
        //        //        image.SaveAsPng(memoryStream);

        //        //        return memoryStream.ToArray();
        //        //    }
        //        //}
        //    }
        //}

       

        

        private bool ContienePalabra(string texto, string palabra)
        {
            // Puedes implementar tu lógica de búsqueda aquí, por ejemplo, utilizando expresiones regulares.
            return texto.Contains(palabra, StringComparison.OrdinalIgnoreCase);
        }

        //public async Task<IActionResult> UpdateFullApproved(int? id, int? filterTypeId)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var requestAssociateMembership = await _context.RequestAssociateMemberships.FindAsync(id);
        //    if (requestAssociateMembership == null)
        //    {
        //        return NotFound();
        //    }

        //    requestAssociateMembership.FullApproved = !requestAssociateMembership.FullApproved;

        //    if (requestAssociateMembership.FullApproved)
        //    {
        //        requestAssociateMembership.Approved = DateTime.Now;
        //        requestAssociateMembership.Rejection = null;
        //        requestAssociateMembership.FullRejection = false;


        //        //_mailHelper.SendMail(requestLicenceSportInternational.Mail, "Aprobación de Solicitud de permiso internacional",
        //        //    _configurationApp.MessageSolicitudApproved);

        //        _mailHelper.SendMail(_configuration["Mail:Admin"], "Aprobación de Solicitud de Membresía para asociados",
        //            _configurationApp.MessageMembresiaAdminApproved + GenerateHtmlTable(requestAssociateMembership, 1));
        //    }
        //    else
        //    {
        //        requestAssociateMembership.Approved = null;
        //    }

        //    requestAssociateMembership.Modify = DateTime.Now;

        //    _context.Update(requestAssociateMembership);
        //    await _context.SaveChangesAsync();

        //    return RedirectToAction(nameof(Index), new { filterTypeId= filterTypeId });
        //}

        public async Task<IActionResult> UpdateFullRejection(int? id, string? motive, int? filterTypeId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestAssociateMembership = await _context.RequestAssociateMemberships.FindAsync(id);
            if (requestAssociateMembership == null)
            {
                return NotFound();
            }

            requestAssociateMembership.FullRejection = !requestAssociateMembership.FullRejection;

            if (requestAssociateMembership.FullRejection)
            {
                requestAssociateMembership.Rejection = DateTime.Now;
                requestAssociateMembership.Approved = null;
                requestAssociateMembership.FullApproved = false;

                _context.Add(new Rejection()
                {
                    Create = DateTime.Now,
                    Motive = motive,
                    RequestAssociateMembershipId = requestAssociateMembership.RequestAssociateMembershipId                    
                });

                await _context.SaveChangesAsync();

                //_mailHelper.SendMail(requestLicenceSportInternational.Mail, "Rechazo de Solicitud de permiso internacional",
                //    _configurationApp.MessageSolicitudRejection);

                _mailHelper.SendMail(_configuration["Mail:Admin"], "Rechazo de Solicitud de Membresía para asociados",
                    _configurationApp.MessageMembresiaAdminRejection + GenerateHtmlTable(requestAssociateMembership, 2) + "<br /> <b>Motivo:</b> <br/>" + motive);
            }
            else
            {
                requestAssociateMembership.Rejection = null;
            }

            requestAssociateMembership.Modify = DateTime.Now;

            _context.Update(requestAssociateMembership);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { filterTypeId=filterTypeId });
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSport = await _context.RequestLicenceSports.FindAsync(id);
            if (requestLicenceSport == null)
            {
                return NotFound();
            }
            ViewData["CountryId"] = new SelectList(_context.Countries, "CountryId", "Name", requestLicenceSport.CountryId);
            return View(requestLicenceSport);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RequestLicenceSport requestLicenceSport)
        {
            if (id != requestLicenceSport.RequestLicenceSportId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(requestLicenceSport);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RequestLicenceSportExists(requestLicenceSport.RequestLicenceSportId))
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
            ViewData["CountryId"] = new SelectList(_context.Countries, "CountryId", "Name", requestLicenceSport.CountryId);
            return View(requestLicenceSport);
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var requestLicenceSport = await _context.RequestLicenceSports.FindAsync(id);
            if (requestLicenceSport != null)
            {
                _context.RequestLicenceSports.Remove(requestLicenceSport);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RequestLicenceSportExists(int id)
        {
            return _context.RequestLicenceSports.Any(e => e.RequestLicenceSportId == id);
        }

        private async Task<IActionResult> GenerateLicenceImage(RequestAssociateMembership requestLicence, string tipo1, string tipo2)
        {
            if (tipo1 == "membresia")
            {
                if (tipo2 == "anverso")
                {
                    return await CreateImageAnverso2(requestLicence.RequestAssociateMembershipId);
                }
                else { 
                    return await CreateImageReverso2(requestLicence.RequestAssociateMembershipId);
                }               
            }
            else 
            {
                if (tipo2 == "anverso")
                {
                    return await CreateAAAAnverso(requestLicence.RequestAssociateMembershipId);
                }
                else
                {
                    return await CreateAAAReverso(requestLicence.RequestAssociateMembershipId);
                }
            }

            return null;
        }

        private async Task<IActionResult> CreateReversoImage(string fondoImagePath)
        {
            using (var fondo = System.Drawing.Image.FromFile(fondoImagePath))
            using (var image = new Bitmap(fondo.Width, fondo.Height))
            using (var graphics = Graphics.FromImage(image))
            using (var memoryStream = new MemoryStream())
            {
                graphics.DrawImage(fondo, 0, 0);
                image.Save(memoryStream, ImageFormat.Png);
                return File(memoryStream.ToArray(), "image/png");
            }
        }

        public async Task<IActionResult> UpdateFullApproved(int? id, int? filterLicenceType)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSport = await _context.RequestAssociateMemberships.FindAsync(id);
            if (requestLicenceSport == null)
            {
                return NotFound();
            }

            requestLicenceSport.FullApproved = !requestLicenceSport.FullApproved;

            if (requestLicenceSport.FullApproved)
            {
                requestLicenceSport.Approved = DateTime.Now;
                requestLicenceSport.Rejection = null;
                requestLicenceSport.FullRejection = false;

                var anversoResult = await GenerateLicenceImage(requestLicenceSport, "membresia", "anverso");
                var anversoAAAResult = await GenerateLicenceImage(requestLicenceSport, "aaa", "anverso");
                var reversoResult = await GenerateLicenceImage(requestLicenceSport, "membresia", "reverso");
                var reversoAAAResult = await GenerateLicenceImage(requestLicenceSport, "aaaa", "reverso");

                string anversoPath = SaveImageFromResult(anversoResult, "anverso", requestLicenceSport.RequestAssociateMembershipId);
                string reversoPath = SaveImageFromResult(reversoResult, "reverso", requestLicenceSport.RequestAssociateMembershipId);
                string anversoAAAPath = SaveImageFromResult(anversoAAAResult, "anversoAAA", requestLicenceSport.RequestAssociateMembershipId);
                string reversoAAAPath = SaveImageFromResult(reversoAAAResult, "reversoAAA", requestLicenceSport.RequestAssociateMembershipId);

                var anversoContent = System.IO.File.ReadAllBytes(anversoPath);
                var reversoContent = System.IO.File.ReadAllBytes(reversoPath);
                var anversoAAAContent = System.IO.File.ReadAllBytes(anversoAAAPath);
                var reversoAAAContent = System.IO.File.ReadAllBytes(reversoAAAPath);

                var attachments = new List<(string FileName, byte[] Content)>
            {
                ("Membresia.png", anversoContent),
                ("Reverso.png", reversoContent),
                ("AnversoAAA.png", anversoAAAContent),
                ("ReversoAAA.png", reversoAAAContent)
            };

                _mailHelper.SendMail(
                    requestLicenceSport.Mail,
                    "Aprobación de Solicitud de Membresía para asociados",
                    _configurationApp.MessageMembresiaApproved + GenerateHtmlTable(requestLicenceSport, 1),
                    attachments
                );

                _mailHelper.SendMail(
                    _configuration["Mail:Admin"],
                    "Aprobación de Solicitud de Membresía para asociados",
                    _configurationApp.MessageMembresiaAdminApproved + GenerateHtmlTable(requestLicenceSport, 1)
                );
            }
            else
            {
                requestLicenceSport.Approved = null;
            }

            requestLicenceSport.Modify = DateTime.Now;

            _context.Update(requestLicenceSport);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { filterLicenceType });
        }

        private string SaveImageFromResult(IActionResult result, string tipo, int requestId)
        {
            var fileContentResult = result as FileContentResult;
            if (fileContentResult == null)
            {
                throw new InvalidOperationException("Invalid image result");
            }

            string directoryPath = Path.Combine(_webHostEnvironment.WebRootPath, "files/membresias");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string imagePath = Path.Combine(directoryPath, $"{requestId}_{tipo}.png");
            System.IO.File.WriteAllBytes(imagePath, fileContentResult.FileContents);

            return imagePath;
        }
    }
}
