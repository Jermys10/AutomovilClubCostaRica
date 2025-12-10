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
using QRCoder;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Drawing.Imaging;
using AutomovilClub.Backend.Enums;
using System.Drawing.Text;
using AutomovilClub.Backend.Extensions;

namespace AutomovilClub.Backend.Controllers
{
    public class RequestVirtualSportsOfficialLicensesController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMailHelper _mailHelper;
        private readonly IConfiguration _configuration;
        private readonly IFontsHelper _fontsHelper;
        private readonly Data.Entities.Configuration _configurationApp;
       
        public RequestVirtualSportsOfficialLicensesController(DataContext context,
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
            var configuraciones = _context.Configurations.ToList();


            if (configuraciones.Count > 0)
            {
                _configurationApp = configuraciones[0];
            }
        }        

        // GET: RequestLicenceSportInternationals
        public async Task<IActionResult> Index(int? filterTypeId=0, string? userFilterId = "-1")
        {
            var dataContext = await _context.RequestVirtualSportsOfficialLicenses.Include(u => u.User).ToListAsync();

            if (userFilterId != null && userFilterId != "-1")
            {
                dataContext = dataContext
                                .Where(r => r.UserId == userFilterId).ToList();
            }

            ViewBag.FilterTypeId = filterTypeId;

            if (filterTypeId == 0)
            {
                dataContext = dataContext
                .Where(r =>  r.FullRejection == false
                     &&  r.FullApproved == false).ToList();
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

            var requestLicenceSport = await _context.RequestVirtualSportsOfficialLicenses.FindAsync(id);
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
                RequestId = requestLicenceSport.RequestVirtualSportsOfficialLicensesId,
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
                    var request = await _context.RequestVirtualSportsOfficialLicenses.FindAsync(model.RequestId);

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

            return View(new RequestVirtualSportsOfficialLicensesViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RequestVirtualSportsOfficialLicensesViewModel model)
        {
            var contentType = model.PhotoFile.ContentType.ToLowerInvariant();
            if (!contentType.StartsWith("image/"))
            {
                ModelState.AddModelError("PhotoFile", "Solo se permiten archivos de imagen (JPG, PNG, etc).");
                // volver con el modelo a la vista
                // Crear la SelectList a partir del enum
                var licenceTypes1 = Enum.GetValues(typeof(LicenceType))
                                     .Cast<LicenceType>()
                                     .Select(s => new SelectListItem
                                     {
                                         Value = ((int)s).ToString(),
                                         Text = s.ToString()
                                     })
                                     .ToList();

                ViewBag.LicenceTypes = licenceTypes1;
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
                var licenceTypes2 = Enum.GetValues(typeof(LicenceType))
                                     .Cast<LicenceType>()
                                     .Select(s => new SelectListItem
                                     {
                                         Value = ((int)s).ToString(),
                                         Text = s.ToString()
                                     })
                                     .ToList();

                ViewBag.LicenceTypes = licenceTypes2;
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var guid = Guid.NewGuid();               
                var nombreArchivoPhoto = "PH_" + guid + ".jpg";

                if (model.PhotoFile==null)
                {
                    ModelState.AddModelError(string.Empty, "La foto es requerida");
                    return View(model);
                }

                var imgPathPhoto = await GuardarArchivoEnServidor(model.PhotoFile, @"files\photos\" + nombreArchivoPhoto);

                if (_configurationApp.ValidateIdentification)
                {
                    
                }

                var requestLicenceSport = new RequestVirtualSportsOfficialLicenses()
                {
                    Identification = model.Identification,
                    Name = model.Name,
                    Mail = model.Mail,
                    PhoneNumber = model.PhoneNumber,
                    Photo = model.PhotoFile!=null ? $"~/files/photos/{nombreArchivoPhoto}" : null,
                    LicenceNumber = model.LicenceNumber,
                    Rol = model.Rol,
                    SignedCodeOfConductAndEthics = model.SignedCodeOfConductAndEthics,
                };

                _context.Add(requestLicenceSport);
                await _context.SaveChangesAsync();

                _mailHelper.SendMail(model.Mail, "Solicitud de oficial deportivo virtual", _configurationApp.MessageOficiales + GenerateHtmlTable(model, 0));

                _mailHelper.SendMail(_configuration["Mail:Admin"], "Solicitud de oficial deportivo virtual", _configurationApp.MessageOficialesAdmin + GenerateHtmlTable(model, 0));

                return RedirectToAction(nameof(Thanks));
            }
            return View(model);
        }



        public async Task<IActionResult> CreateImageAnverso(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestVirtualSportsOfficialLicenses.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaDeportivaOficial", "Licencia_Oficial_02.png");
            var fotoRecienteUrl = requestLicence.PhotoFullPath;


            using (HttpClient client = new HttpClient())
            using (Stream fotoStream = await client.GetStreamAsync(fotoRecienteUrl))
            using (var fondo = System.Drawing.Image.FromFile(fondoImage))
            using (var foto = System.Drawing.Image.FromStream(fotoStream))
            using (var image = new Bitmap(fondo.Width, fondo.Height))
            using (var imageOriginalSize = new Bitmap(2048, 3125))
            using (var graphicsOriginal = Graphics.FromImage(imageOriginalSize))
            using (var graphics = Graphics.FromImage(image))
            using (var imageFinal = new Bitmap(2048, 3125))
            using (var graphicsFinal = Graphics.FromImage(imageFinal))
            using (var memoryStream = new MemoryStream())
            {
                graphics.DrawImage(fondo, 0, 0, 2084, 3125);

                graphicsOriginal.SmoothingMode = graphicsFinal.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphicsOriginal.InterpolationMode = graphicsFinal.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphicsOriginal.PixelOffsetMode = graphicsFinal.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphicsOriginal.CompositingQuality = graphicsFinal.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                // 1. Trabajar en tamaño original (2048x3125)
                graphicsOriginal.DrawImage(fondo, 0, 0, 2048, 3125);

                System.Drawing.Rectangle photoRect = new System.Drawing.Rectangle(760, 150, 560, 785);
                int cornerRadius = 20;
                int borderSize = 5;

                System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(photoRect.X, photoRect.Y, cornerRadius, cornerRadius, 180, 90);
                path.AddArc(photoRect.X + photoRect.Width - cornerRadius, photoRect.Y, cornerRadius, cornerRadius, 270, 90);
                path.AddArc(photoRect.X + photoRect.Width - cornerRadius, photoRect.Y + photoRect.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
                path.AddArc(photoRect.X, photoRect.Y + photoRect.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
                path.CloseFigure();

                using (var fillBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                {
                    graphics.FillPath(fillBrush, path);
                }

                using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.White, borderSize))
                {
                    graphics.DrawPath(borderPen, path);
                }

                System.Drawing.Rectangle photoRectWithBorder = System.Drawing.Rectangle.Inflate(photoRect, -borderSize, -borderSize);
                graphics.DrawImage(foto, photoRectWithBorder);
                graphics.ResetClip();

                // Define the text to be drawn on the image from the requestLicence object
                string nombreCompleto = !string.IsNullOrEmpty(requestLicence.ShortName) ? requestLicence.ShortName : requestLicence.Name; // Reemplazar con la propiedad real de requestLicence                
                string numeroLicencia = requestLicence.LicenceNumber;

                // Utiliza diferentes fuentes y tamaños según la necesidad
                using (var fontTitle = _fontsHelper.GetFont("OPTIEdgar-Extended", 92, System.Drawing.FontStyle.Bold))
                using (var fontName = _fontsHelper.GetFont("Visby CF Extra Bold", 92, System.Drawing.FontStyle.Bold))
                using (var fontData = _fontsHelper.GetFont("Visby CF Extra Bold", 62, System.Drawing.FontStyle.Bold))
                using (var fontLabel = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold))
                using (var fontBodySmall = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
                using (var fontBody = _fontsHelper.GetFont("Visby CF Extra Bold", 62, System.Drawing.FontStyle.Bold))
                using (var fontBody2 = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
                using (var fontEquis = new System.Drawing.Font("Arial", 64, System.Drawing.FontStyle.Bold))
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                using (var brushBlack = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2))
                {
                    var imageWidth = fondo.Width;
                    var availableWidth = imageWidth - 30; // Espacio disponible considerando un margen de 30 píxeles a cada lado

                    var rightEdge = 900; // 30 píxeles de margen derecho

                    var licenceNumberSize = graphics.MeasureString(numeroLicencia, fontBody);
                    var rolSize = graphics.MeasureString(requestLicence.Rol.ToFriendlyString(), fontBody);
                    var gradeSize = graphics.MeasureString(requestLicence.Grade.ToString(), fontBody);
                    var expeditionSize = graphics.MeasureString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody);
                    var expirationSize = graphics.MeasureString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody);

                    // Calcular las posiciones para centrar el texto
                    var centerX = imageWidth / 2;

                    // Dibujar el nombre completo centrado
                    //graphics.DrawString(firstLine, fontTitle, brush, new System.Drawing.PointF(centerX - firstLineSize.Width / 2, 980));
                    var nameSize = graphicsOriginal.MeasureString(nombreCompleto, fontName);
                    float nameX = (2048 - nameSize.Width) / 2;
                    graphics.DrawString(nombreCompleto, fontName, brush, nameX, 1050);

                    float currentY = 1305;

                    graphics.DrawString(numeroLicencia, fontBody, brush, rightEdge, currentY);

                    currentY += 142;

                    graphics.DrawString(requestLicence.Rol.ToFriendlyString(), fontBody, brush, rightEdge, currentY);

                    currentY += 142;
                    graphics.DrawString(requestLicence.Grade.ToString(), fontBody, brush, rightEdge, currentY);

                    currentY += 144;
                    graphics.DrawString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);

                    currentY += 148;
                    graphics.DrawString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);
                }

                string datosCompletos = $"Licencia aprobada y autorizada por el Automóvil Club de Costa Rica.";


                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(datosCompletos, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);

                byte[] qrCodeImageBytes = qrCode.GetGraphic(
                    pixelsPerModule: 20,
                    darkColorRgba: new byte[] { 255, 255, 255, 255 }, // módulos blancos
                    lightColorRgba: new byte[] { 0, 0, 0, 0 }, // fondo transparente
                    drawQuietZones: true
                );

                // Convertir el array de bytes a un objeto Image
                using (MemoryStream ms = new MemoryStream(qrCodeImageBytes))
                {
                    System.Drawing.Image qrImage = System.Drawing.Image.FromStream(ms);

                    // Redondear los bordes del código QR
                    int qrWidth = 410;
                    int qrHeight = 410;
                    int qrX = 840; // Posición en el eje X
                    int qrY = 2050; // Posición en el eje Y
                    int qrCornerRadius = 15;

                    using (GraphicsPath qrPath = new GraphicsPath())
                    {
                        qrPath.AddArc(qrX, qrY, qrCornerRadius, qrCornerRadius, 180, 90);
                        qrPath.AddArc(qrX + qrWidth - qrCornerRadius, qrY, qrCornerRadius, qrCornerRadius, 270, 90);
                        qrPath.AddArc(qrX + qrWidth - qrCornerRadius, qrY + qrHeight - qrCornerRadius, qrCornerRadius, qrCornerRadius, 0, 90);
                        qrPath.AddArc(qrX, qrY + qrHeight - qrCornerRadius, qrCornerRadius, qrCornerRadius, 90, 90);
                        qrPath.CloseFigure();

                        graphics.SetClip(qrPath);
                        graphics.DrawImage(qrImage, new System.Drawing.Rectangle(qrX, qrY, qrWidth, qrHeight));
                        graphics.ResetClip();
                    }
                }
            

            image.Save(memoryStream, ImageFormat.Png);
                return File(memoryStream.ToArray(), "image/png");
            }
        }

        public async Task<IActionResult> EditPartial(int? id, int? filterTypeId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSport = await _context.RequestVirtualSportsOfficialLicenses.FindAsync(id);
            if (requestLicenceSport == null)
            {
                return NotFound();
            }
            var licenceTypes = Enum.GetValues(typeof(Rol))
                               .Cast<Rol>()
                               .Select(s => new SelectListItem
                               {
                                   Value = ((int)s).ToString(),
                                   Text = s.ToString()
                               })
                               .ToList();

            ViewBag.LicenceTypes = licenceTypes;


            var model = new RequestVirtualSportsOfficialLicensesViewModelShort()
            {
                Expedition = requestLicenceSport.Expedition,
                Expiration = requestLicenceSport.Expiration,
                Grade = requestLicenceSport.Grade,
                ShortName = requestLicenceSport.ShortName,
                RequestVirtualSportsOfficialLicensesId =requestLicenceSport.RequestVirtualSportsOfficialLicensesId,
                LicenceNumber=requestLicenceSport.LicenceNumber,
                FilterTypeId=filterTypeId,
                Photo = requestLicenceSport.Photo,
            };

            if (model.ShortName == null)
                model.ShortName = requestLicenceSport.Name;

            return View("_EditPartial", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPartial(RequestVirtualSportsOfficialLicensesViewModelShort model)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    var pic = model.Photo;
                    var guid = Guid.NewGuid();
                    var nombreArchivoPhoto = "PH_" + guid + ".jpg";

                    var requestLicenceSport = await _context.RequestVirtualSportsOfficialLicenses.FindAsync(model.RequestVirtualSportsOfficialLicensesId);

                    requestLicenceSport.Expedition = model.Expedition;
                    requestLicenceSport.Expiration = model.Expiration;
                    requestLicenceSport.Grade = model.Grade;
                    requestLicenceSport.LicenceNumber = model.LicenceNumber;
                    requestLicenceSport.ShortName = model.ShortName;
                    // Lógica para leer y validar documentos PDF
                    if (model.PhotoFile != null)
                    {
                        // Guardar el archivo en el servidor
                        var imgPathPhoto = await GuardarArchivoEnServidor(model.PhotoFile, @"files\photos\" + nombreArchivoPhoto);
                        model.Photo = imgPathPhoto;
                    }

                    requestLicenceSport.Photo = model.PhotoFile != null ? $"~/files/photos/{nombreArchivoPhoto}" : model.Photo;

                    _context.Update(requestLicenceSport);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                   
                }
                return RedirectToAction(nameof(Index), new { model.FilterTypeId });
            }

            return View(model);
        }

        private void DrawCheckbox(Graphics graphics, System.Drawing.Pen pen, System.Drawing.Brush brush, System.Drawing.Font font, System.Drawing.Point position, int size, bool isChecked)
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(position.X, position.Y, size, size);
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = 3; // smaller radius for rounded corners
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();

            graphics.FillPath(System.Drawing.Brushes.White, path);
            graphics.DrawPath(pen, path);

            if (isChecked)
            {
                graphics.DrawString("X", font, brush, position.X + 1, position.Y - 1);
            }
        }


        public async Task<IActionResult> CreateImageReverso(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestVirtualSportsOfficialLicenses.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaDeportivaOficial", "LDF.jpg");

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



        public string GenerateHtmlTable(RequestVirtualSportsOfficialLicenses model, int estado)
        {
            string tableHtml = @"
        <table style='border-collapse: collapse; width: 100%; margin-top: 20px;'>";

            // Agregar filas con los datos del modelo
            tableHtml += AddTableRow("Identificación", model.Identification);
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


        //public async Task<IActionResult> UpdateFullApproved(int? id, int? filterTypeId)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var requestLicenceSportInternational = await _context.RequestVirtualSportsOfficialLicenses.FindAsync(id);
        //    if (requestLicenceSportInternational == null)
        //    {
        //        return NotFound();
        //    }
            
        //    requestLicenceSportInternational.FullApproved = !requestLicenceSportInternational.FullApproved;

        //    if (requestLicenceSportInternational.FullApproved)
        //    {
        //        requestLicenceSportInternational.Approved = DateTime.Now;
        //        requestLicenceSportInternational.Rejection = null;
        //        requestLicenceSportInternational.FullRejection = false;


        //        _mailHelper.SendMail(requestLicenceSportInternational.Mail, "Aprobación de Solicitud de Oficial Deportivo Virtual", 
        //            _configurationApp.MessagePermisoApproved);

        //        _mailHelper.SendMail(_configuration["Mail:Admin"], "Aprobación de Solicitud de Oficial Deportivo Virtual",
        //            _configurationApp.MessagePermisoAdminAprobado);
        //    }
        //    else 
        //    {
        //        requestLicenceSportInternational.Approved= null;
        //    }

        //    requestLicenceSportInternational.Modify = DateTime.Now;

        //    _context.Update(requestLicenceSportInternational);
        //    await _context.SaveChangesAsync();

        //    return RedirectToAction(nameof(Index), new { filterTypeId = filterTypeId });
        //}

        public async Task<IActionResult> UpdateFullRejection(int? id, string? motive, int? filterTypeId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSportInternational = await _context.RequestVirtualSportsOfficialLicenses.FindAsync(id);
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
                    RequestVirtualSportsOfficialLicensesId = requestLicenceSportInternational.RequestVirtualSportsOfficialLicensesId
                });

                await _context.SaveChangesAsync();

                _mailHelper.SendMail(requestLicenceSportInternational.Mail, "Rechazo de Solicitud de permiso internacional", 
                    _configurationApp.MessagePermisoRejection + "<br /> <b>Motivo:</b> <br/>" + motive);

                _mailHelper.SendMail(_configuration["Mail:Admin"], "Rechazo de Solicitud de permiso internacional",
                    _configurationApp.MessagePermisoAdminRejection + "<br /> <b>Motivo:</b> <br/>" + motive);
            }
            else
            {
                requestLicenceSportInternational.Rejection = null;
            }

            requestLicenceSportInternational.Modify = DateTime.Now;

            _context.Update(requestLicenceSportInternational);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { filterTypeId= filterTypeId });
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

        private async Task<IActionResult> GenerateLicenceImage(RequestVirtualSportsOfficialLicenses requestLicence, string tipo)
        {
            if (tipo == "anverso")
            {
                return await CreateImageAnverso(requestLicence.RequestVirtualSportsOfficialLicensesId);
            }
            else
            {
                return await CreateImageReverso(requestLicence.RequestVirtualSportsOfficialLicensesId);
            }
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

        public async Task<IActionResult> UpdateFullApproved(int? id, int? filterTypeId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSport = await _context.RequestVirtualSportsOfficialLicenses.FindAsync(id);
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

                var anversoResult = await GenerateLicenceImage(requestLicenceSport, "anverso");
                //var reversoResult = await GenerateLicenceImage(requestLicenceSport, "reverso");

                string anversoPath = SaveImageFromResult(anversoResult, "anverso", requestLicenceSport.RequestVirtualSportsOfficialLicensesId);
                //string reversoPath = SaveImageFromResult(reversoResult, "reverso", requestLicenceSport.RequestVirtualSportsOfficialLicensesId);

                var anversoContent = System.IO.File.ReadAllBytes(anversoPath);
                //var reversoContent = System.IO.File.ReadAllBytes(reversoPath);

                var attachments = new List<(string FileName, byte[] Content)>
            {
                ("Licencia de Oficiales Deportivos.png", anversoContent)
                //("Reverso.png", reversoContent)
            };

                _mailHelper.SendMail(
                    requestLicenceSport.Mail,
                    "Aprobación de Solicitud de Oficial Deportivo Virtual",
                    _configurationApp.MessageOficialesApproved + GenerateHtmlTable(requestLicenceSport, 1),
                    attachments
                );

                _mailHelper.SendMail(
                    _configuration["Mail:Admin"],
                    "Aprobación de Solicitud de Oficial Deportivo Virtual",
                    _configurationApp.MessageOficialesAdminApproved + GenerateHtmlTable(requestLicenceSport, 1)
                );
            }
            else
            {
                requestLicenceSport.Approved = null;
            }

            requestLicenceSport.Modify = DateTime.Now;

            _context.Update(requestLicenceSport);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { filterTypeId });
        }

        private string SaveImageFromResult(IActionResult result, string tipo, int requestId)
        {
            var fileContentResult = result as FileContentResult;
            if (fileContentResult == null)
            {
                throw new InvalidOperationException("Invalid image result");
            }

            string directoryPath = Path.Combine(_webHostEnvironment.WebRootPath, "files/licences-concursante");
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
