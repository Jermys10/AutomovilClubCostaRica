using AutomovilClub.Backend.Data;
using AutomovilClub.Backend.Data.Entities;
using AutomovilClub.Backend.Enums;
using AutomovilClub.Backend.Extensions;
using AutomovilClub.Backend.Helpers;
using AutomovilClub.Backend.Models;
using IronOcr;
using IronSoftware.Drawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;



namespace AutomovilClub.Backend.Controllers
{
    public class RequestLicenceConcursanteSportsController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMailHelper _mailHelper;
        private readonly IConfiguration _configuration;
        private readonly IFontsHelper _fontsHelper;
        private readonly Data.Entities.Configuration _configurationApp;

        public RequestLicenceConcursanteSportsController(DataContext context, 
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


        public async Task<IActionResult> Index(int? filterTypeId = 0, int? filterLicenceType=2, string? userFilterId = "-1")
        {
            var requestLicenceSports = await _context.RequestLicenceConcursanteSports.Include(u => u.User).ToListAsync();


            if (userFilterId != null && userFilterId != "-1")
            {
                requestLicenceSports = requestLicenceSports
                                .Where(r => r.UserId == userFilterId).ToList();
            }

            if (filterTypeId == 0)
            {
                requestLicenceSports = requestLicenceSports
                .Where(r => r.FullRejection == false
                     && r.FullApproved == false).ToList();
            }

            if (filterTypeId == 1)
            {
                requestLicenceSports= requestLicenceSports
                                   .Where(r => r.FullRejection == false
                                                       && r.FullApproved == true).ToList();
            }

            if (filterTypeId == 2)
            {
                requestLicenceSports= requestLicenceSports
                                                      .Where(r => r.FullRejection == true && r.FullApproved == false).ToList();
            }

            // Crear la SelectList a partir del enum
            var licenceTypes = Enum.GetValues(typeof(LicenceType))
                                 .Cast<LicenceType>()
                                 .Select(s => new SelectListItem
                                 {
                                     Value = ((int)s).ToString(),
                                     Text = s.ToString()
                                 })
                                 .ToList();

            ViewBag.LicenceTypes = licenceTypes;

            if (filterLicenceType!=null)
            {
                if (filterLicenceType!=2)
                {
                    requestLicenceSports = requestLicenceSports
                                                      .Where(r => r.LicenceType == (LicenceType)filterLicenceType).ToList();
                }                               
            }

            ViewData["UserId"] = new SelectList(
                 new[] { new { Id = "-1", Name = "Todos" } }
                 .Concat(_context.Users.Where(u => !u.IsDelete)
                     .AsEnumerable() // Operación en memoria
                     .Select(user => new { Id = user.Id.ToString(), Name = user.Name }))
                 .ToList(),
                 "Id",
                 "Name",
                 userFilterId
             );

            ViewBag.FilterTypeId = filterTypeId;
            ViewBag.FilterLicenceType = filterLicenceType;

            return View(requestLicenceSports.ToList());
        }

        public async Task<IActionResult> ChangeAssigned(int? id, int? filterTypeId, int? filterLicenceType)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSport = await _context.RequestLicenceConcursanteSports.FindAsync(id);
            if (requestLicenceSport == null)
            {
                return NotFound();
            }

            ViewData["UserId"] = new SelectList(
                  _context.Users
                      .AsEnumerable()  // Cambia a operaciones de cliente en memoria                      
                      .ToList(),
                  "Id",
                  "Name"
              );


            var model = new RequestViewModelShort()
            {
                RequestId = requestLicenceSport.RequestLicenceConcursanteSportId,
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
                    var request = await _context.RequestLicenceConcursanteSports.FindAsync(model.RequestId);

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

            var requestLicenceSport = await _context.RequestLicenceConcursanteSports
                .FirstOrDefaultAsync(m => m.RequestLicenceConcursanteSportId == id);
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
            // Crear la SelectList a partir del enum
            var licenceTypes = Enum.GetValues(typeof(LicenceType))
                                 .Cast<LicenceType>()
                                 .Select(s => new SelectListItem
                                 {
                                     Value = ((int)s).ToString(),
                                     Text = s.ToString()
                                 })
                                 .ToList();

            ViewBag.LicenceTypes = licenceTypes;
            return View(new RequestLicenceConcursanteSportViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RequestLicenceConcursanteSportViewModel model)
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

                // Lógica para leer y validar documentos PDF
                if (model.PhotoFile != null)
                {   
                    // Guardar el archivo en el servidor
                    var imgPathPhoto= await GuardarArchivoEnServidor(model.PhotoFile, @"files\photos\" + nombreArchivoPhoto);
                    model.Photo= imgPathPhoto;
                }

                var requestLicenceSport = new RequestLicenceConcursanteSport()
                {
                    Name = model.Name,
                    Mail = model.Mail,
                    PhoneNumber = model.PhoneNumber,
                    Photo = model.PhotoFile != null ? $"~/files/photos/{nombreArchivoPhoto}" : null,
                    LicenceNumber =  model.LicenceNumber,
                    BirthDay=model.BirthDay,
                    LicenceType=model.LicenceType
                };
                _context.Add(requestLicenceSport);
                await _context.SaveChangesAsync();

                _mailHelper.SendMail(model.Mail, "Solicitud de licencia deportiva nacional concursante", 
                    _configurationApp.MessageSolicitud + GenerateHtmlTable(model, 0));

                _mailHelper.SendMail(_configuration["Mail:Admin"], "Nueva solicitud de licencia deportiva nacional concursante", 
                    _configurationApp.MessageSolicitudAdmin);

                return RedirectToAction(nameof(Thanks));
            }


            // Crear la SelectList a partir del enum
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

        public string GenerateHtmlTable(RequestLicenceConcursanteSportViewModel model, int estado)
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

        public string GenerateHtmlTable(RequestLicenceConcursanteSport model, int estado)
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


        public async Task<IActionResult> CreateImageAnversoBK(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestLicenceConcursanteSports.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }

            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaNacionalConcursante", "Licencia_Concursante_Nacional_02.png");
            var fotoRecienteUrl = requestLicence.PhotoFullPath;
            //var pathLogo1 = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaNacionalConcursante", "ICONO2.png");

            if (requestLicence.Expedition != null && requestLicence.Expiration != null)
            {
                //using (var logo1 = System.Drawing.Image.FromFile(pathLogo1))
                using (HttpClient client = new HttpClient())
                using (Stream fotoStream = await client.GetStreamAsync(fotoRecienteUrl))
                using (var fondo = System.Drawing.Image.FromFile(fondoImage))
                using (var foto = System.Drawing.Image.FromStream(fotoStream))
                using (var image = new Bitmap(fondo.Width, fondo.Height))
                using (var graphics = Graphics.FromImage(image))
                using (var memoryStream = new MemoryStream())
                {
                    graphics.DrawImage(fondo, 0, 0);

                    System.Drawing.Rectangle photoRect = new System.Drawing.Rectangle(170, 460, 160, 218);
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

                    string nombreCompleto = !string.IsNullOrEmpty(requestLicence.ShortName) ? requestLicence.ShortName : requestLicence.Name;
                    string numeroLicencia = requestLicence.LicenceNumber;

                    using (var fontTitle = _fontsHelper.GetFont("OPTIEdgar-Extended", 18, System.Drawing.FontStyle.Bold))
                    using (var fontLabel = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold))
                    using (var fontBodySmall = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
                    using (var fontBody = _fontsHelper.GetFont("Visby CF Extra Bold", 18, System.Drawing.FontStyle.Bold))
                    using (var fontBody2 = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
                    using (var fontEquis = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Bold))
                    using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                    using (var brushBlack = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2))
                    {
                        var imageWidth = fondo.Width;
                        var availableWidth = imageWidth - 130; // Espacio disponible considerando un margen de 30 píxeles a cada lado

                        // Divide el nombre en dos líneas si es necesario
                        var (firstLine, secondLine) = StringExtensions.SplitName(nombreCompleto, availableWidth, graphics, fontTitle);

                        var firstLineSize = graphics.MeasureString(firstLine, fontTitle);
                        var secondLineSize = graphics.MeasureString(secondLine, fontTitle);
                        var licenceNumberSize = graphics.MeasureString(numeroLicencia, fontBody);
                        var birthDateSize = graphics.MeasureString(requestLicence.BirthDay.Value.ToString("d/M/yyyy"), fontBody);
                        var expeditionSize = graphics.MeasureString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody);
                        var expirationSize = graphics.MeasureString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody);

                        // Calcular las posiciones para centrar el texto
                        var centerX = imageWidth / 2;

                        // Dibujar el nombre completo centrado

                        //var y1 = 142;
                        //var y2 = 162;

                        graphics.DrawString(firstLine, fontTitle, brush, new System.Drawing.PointF(centerX - firstLineSize.Width / 2, !string.IsNullOrEmpty(secondLine) ? 127 : 142));
                        if (!string.IsNullOrEmpty(secondLine))
                        {
                            graphics.DrawString(secondLine, fontTitle, brush, new System.Drawing.PointF(centerX - secondLineSize.Width / 2, 157));
                        }

                        // Dibujar los demás textos alineados a la derecha
                        var rightEdge = imageWidth - 30; // 30 píxeles de margen derecho
                        graphics.DrawString(numeroLicencia, fontBody, brush, new System.Drawing.PointF(rightEdge - licenceNumberSize.Width, 245));
                        graphics.DrawString(requestLicence.BirthDay.Value.ToString("M/d/yyyy"), fontBody, brush, new System.Drawing.PointF(rightEdge - birthDateSize.Width, 290));
                        graphics.DrawString(requestLicence.Expedition.Value.ToString("M/d/yyyy"), fontBody, brush, new System.Drawing.PointF(rightEdge - expeditionSize.Width, 332));
                        graphics.DrawString(requestLicence.Expiration.Value.ToString("M/d/yyyy"), fontBody, brush, new System.Drawing.PointF(rightEdge - expirationSize.Width, 378));
                    }

                    image.Save(memoryStream, ImageFormat.Png);
                    return File(memoryStream.ToArray(), "image/png");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CreateImageAnverso1(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestLicenceConcursanteSports.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }

            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaNacionalConcursante", "Licencia_Concursante_Nacional_02.png");
            var fotoRecienteUrl = requestLicence.PhotoFullPath;

            if (requestLicence.Expedition != null && requestLicence.Expiration != null)
            {
                using (HttpClient client = new HttpClient())
                using (Stream fotoStream = await client.GetStreamAsync(fotoRecienteUrl))
                using (var fondo = System.Drawing.Image.FromFile(fondoImage))
                using (var foto = System.Drawing.Image.FromStream(fotoStream))
                using (var image = new Bitmap(fondo.Width, fondo.Height))
                using (var graphics = Graphics.FromImage(image))
                using (var memoryStream = new MemoryStream())
                {
                    // Configuración de calidad
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                    // Dibujar fondo
                    graphics.DrawImage(fondo, 0, 0, fondo.Width, fondo.Height);

                    // Proporciones basadas en el tamaño de la imagen (2048x3125)
                    float scaleX = fondo.Width / 2084;
                    float scaleY = fondo.Height / 3125f;

                    // Posición y tamaño de la foto (ajustado para 2048x3125)
                    System.Drawing.Rectangle photoRect = new System.Drawing.Rectangle(
                        (int)(170 * scaleX),
                        (int)(460 * scaleY),
                        (int)(160 * scaleX),
                        (int)(218 * scaleY));

                    // Dibujar foto con bordes redondeados
                    int cornerRadius = (int)(20 * Math.Min(scaleX, scaleY));
                    int borderSize = (int)(5 * Math.Min(scaleX, scaleY));

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
                    graphics.SetClip(path);
                    graphics.DrawImage(foto, photoRectWithBorder);
                    graphics.ResetClip();

                    // Textos
                    string nombreCompleto = !string.IsNullOrEmpty(requestLicence.ShortName) ? requestLicence.ShortName : requestLicence.Name;
                    string numeroLicencia = requestLicence.LicenceNumber;

                    // Fuentes escaladas
                    using (var fontTitle = _fontsHelper.GetFont("OPTIEdgar-Extended", 24 * scaleY, System.Drawing.FontStyle.Bold))
                    using (var fontLabel = new System.Drawing.Font("Arial", 12 * scaleY, System.Drawing.FontStyle.Bold))
                    using (var fontBodySmall = new System.Drawing.Font("Arial", 8 * scaleY, System.Drawing.FontStyle.Regular))
                    using (var fontBody = _fontsHelper.GetFont("Visby CF Extra Bold", 18 * scaleY, System.Drawing.FontStyle.Bold))
                    using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                    {
                        // Dividir nombre si es necesario
                        var (firstLine, secondLine) = StringExtensions.SplitName(nombreCompleto, fondo.Width - (int)(100 * scaleX), graphics, fontTitle);

                        // Posicionamiento de textos (basado en el diseño de 2048x3125)
                        float centerX = fondo.Width / 2;

                        // Nombre
                        var firstLineSize = graphics.MeasureString(firstLine, fontTitle);
                        graphics.DrawString(firstLine, fontTitle, brush, centerX - firstLineSize.Width / 2, 300 * scaleY);

                        if (!string.IsNullOrEmpty(secondLine))
                        {
                            var secondLineSize = graphics.MeasureString(secondLine, fontTitle);
                            graphics.DrawString(secondLine, fontTitle, brush, centerX - secondLineSize.Width / 2, 157 * scaleY);
                        }

                        // Información de la licencia (alineado a la derecha)
                        float rightMargin = fondo.Width - (30 * scaleX);

                        // Número de licencia
                        var licenceNumberSize = graphics.MeasureString(numeroLicencia, fontBody);
                        graphics.DrawString(numeroLicencia, fontBody, brush, rightMargin - licenceNumberSize.Width, 245 * scaleY);

                        // Fecha de nacimiento
                        var birthDateSize = graphics.MeasureString(requestLicence.BirthDay.Value.ToString("d/M/yyyy"), fontBody);
                        graphics.DrawString(requestLicence.BirthDay.Value.ToString("d/M/yyyy"), fontBody, brush, rightMargin - birthDateSize.Width, 290 * scaleY);

                        // Fecha de expedición
                        var expeditionSize = graphics.MeasureString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody);
                        graphics.DrawString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody, brush, rightMargin - expeditionSize.Width, 332 * scaleY);

                        // Fecha de vencimiento
                        var expirationSize = graphics.MeasureString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody);
                        graphics.DrawString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody, brush, rightMargin - expirationSize.Width, 378 * scaleY);
                    }

                    image.Save(memoryStream, ImageFormat.Png);
                    return File(memoryStream.ToArray(), "image/png");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CreateImageAnverso(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestLicenceConcursanteSports.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }

            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaNacionalConcursante", "Licencia_Concursante_Nacional_02.png");
            var fotoRecienteUrl = requestLicence.PhotoFullPath;

            if (requestLicence.Expedition != null && requestLicence.Expiration != null)
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(fotoRecienteUrl);
                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"No se pudo descargar la imagen: {response.StatusCode}");

                    using (var fotoStream = await response.Content.ReadAsStreamAsync())
                    using (var memoryStreamFoto = new MemoryStream())
                    {

                        await fotoStream.CopyToAsync(memoryStreamFoto);
                        memoryStreamFoto.Position = 0;

                        // Validar cabecera binaria (JPG o PNG)
                        byte[] header = new byte[4];
                        memoryStreamFoto.Read(header, 0, header.Length);
                        memoryStreamFoto.Position = 0;

                        bool isJpeg = header[0] == 0xFF && header[1] == 0xD8;
                        bool isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

                        if (!isJpeg && !isPng)
                        {
                            return BadRequest("La imagen descargada no es un JPG ni un PNG válido.");
                        }

                        System.Drawing.Image foto;
                        try
                        {
                            foto = System.Drawing.Image.FromStream(memoryStreamFoto);
                        }
                        catch (Exception ex)
                        {
                            return BadRequest($"No se pudo procesar la imagen descargada: {ex.Message}");
                        }

                        using (var fondoOriginal = System.Drawing.Image.FromFile(fondoImage))

                        // Primero creamos la imagen en tamaño original (2048x3125)
                        using (var imageOriginalSize = new Bitmap(2084, 3125))
                        using (var graphicsOriginal = Graphics.FromImage(imageOriginalSize))
                        // Luego creamos la imagen final redimensionada (502x750)
                        using (var imageFinal = new Bitmap(502, 750))
                        using (var graphicsFinal = Graphics.FromImage(imageFinal))
                        using (var memoryStream = new MemoryStream())
                        {
                            // Configuración de calidad para ambas operaciones
                            graphicsOriginal.SmoothingMode = graphicsFinal.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                            graphicsOriginal.InterpolationMode = graphicsFinal.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            graphicsOriginal.PixelOffsetMode = graphicsFinal.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            graphicsOriginal.CompositingQuality = graphicsFinal.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                            // 1. Trabajar en tamaño original (2048x3125)
                            graphicsOriginal.DrawImage(fondoOriginal, 0, 0, 2084, 3125);

                            // ===== FOTO DEL TRAMITANTE (medidas originales) =====
                            System.Drawing.Rectangle photoRect = new System.Drawing.Rectangle(184, 1908, 618, 862);

                            // Bordes redondeados (13px como especificado)
                            int cornerRadius = 13;
                            int borderSize = 2;

                            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                            path.AddArc(photoRect.X, photoRect.Y, cornerRadius, cornerRadius, 180, 90);
                            path.AddArc(photoRect.X + photoRect.Width - cornerRadius, photoRect.Y, cornerRadius, cornerRadius, 270, 90);
                            path.AddArc(photoRect.X + photoRect.Width - cornerRadius, photoRect.Y + photoRect.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
                            path.AddArc(photoRect.X, photoRect.Y + photoRect.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
                            path.CloseFigure();

                            using (var fillBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                            {
                                graphicsOriginal.FillPath(fillBrush, path);
                            }

                            using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.White, borderSize))
                            {
                                graphicsOriginal.DrawPath(borderPen, path);
                            }

                            System.Drawing.Rectangle photoRectWithBorder = System.Drawing.Rectangle.Inflate(photoRect, -borderSize, -borderSize);
                            graphicsOriginal.SetClip(path);
                            graphicsOriginal.DrawImage(foto, photoRectWithBorder);
                            graphicsOriginal.ResetClip();

                            // ===== TEXTOS (medidas originales) =====
                            string nombreCompleto = !string.IsNullOrEmpty(requestLicence.ShortName) ? requestLicence.ShortName : requestLicence.Name;
                            string numeroLicencia = requestLicence.LicenceNumber;

                            // Fuentes con tamaños originales (29pt para nombre, 17pt para datos)
                            using (var fontName = _fontsHelper.GetFont("Visby CF Extra Bold", 92, System.Drawing.FontStyle.Bold))
                            using (var fontData = _fontsHelper.GetFont("Visby CF Extra Bold", 62, System.Drawing.FontStyle.Bold))
                            using (var brushWhite = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 255, 255)))
                            {
                                // ===== NOMBRE CENTRADO =====
                                var nameSize = graphicsOriginal.MeasureString(nombreCompleto, fontName);
                                float nameX = (2084 - nameSize.Width) / 2; // Centrado en 2048px
                                graphicsOriginal.DrawString(nombreCompleto, fontName, brushWhite, nameX, 770);

                                // ===== DATOS ALINEADOS A LA DERECHA =====
                                float rightMargin = 980 - 10; // 30px del borde derecho
                                float currentY = 1065;

                                // Número de licencia (incluye etiqueta)
                                string licenceText = $"{numeroLicencia}";
                                var licenceSize = graphicsOriginal.MeasureString(licenceText, fontData);
                                graphicsOriginal.DrawString(licenceText, fontData, brushWhite, rightMargin, currentY);
                                currentY += 142;

                                // Fecha de nacimiento
                                string birthText = $"{requestLicence.BirthDay.Value.ToString("d/M/yyyy")}";
                                var birthSize = graphicsOriginal.MeasureString(birthText, fontData);
                                graphicsOriginal.DrawString(birthText, fontData, brushWhite, rightMargin, currentY);
                                currentY += 142;

                                // Fecha de expedición
                                string expeditionText = $"{requestLicence.Expedition.Value.ToString("d/M/yyyy")}";
                                var expeditionSize = graphicsOriginal.MeasureString(expeditionText, fontData);
                                graphicsOriginal.DrawString(expeditionText, fontData, brushWhite, rightMargin, currentY);
                                currentY += 147;

                                // Fecha de vencimiento
                                string expirationText = $"{requestLicence.Expiration.Value.ToString("d/M/yyyy")}";
                                var expirationSize = graphicsOriginal.MeasureString(expirationText, fontData);
                                graphicsOriginal.DrawString(expirationText, fontData, brushWhite, rightMargin, currentY);
                            }

                            // ===== QR CODE (medidas originales) =====
                            // int qrSize = 93; // Tamaño original
                            // Rectangle qrRect = new Rectangle(x, y, qrSize, qrSize);
                            // Implementar generación y dibujo del QR aquí

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
                                int qrX = 1295; // Posición en el eje X
                                int qrY = 2100; // Posición en el eje Y
                                int qrCornerRadius = 15;

                                using (GraphicsPath qrPath = new GraphicsPath())
                                {
                                    qrPath.AddArc(qrX, qrY, qrCornerRadius, qrCornerRadius, 180, 90);
                                    qrPath.AddArc(qrX + qrWidth - qrCornerRadius, qrY, qrCornerRadius, qrCornerRadius, 270, 90);
                                    qrPath.AddArc(qrX + qrWidth - qrCornerRadius, qrY + qrHeight - qrCornerRadius, qrCornerRadius, qrCornerRadius, 0, 90);
                                    qrPath.AddArc(qrX, qrY + qrHeight - qrCornerRadius, qrCornerRadius, qrCornerRadius, 90, 90);
                                    qrPath.CloseFigure();

                                    graphicsOriginal.SetClip(qrPath);
                                    graphicsOriginal.DrawImage(qrImage, new System.Drawing.Rectangle(qrX, qrY, qrWidth, qrHeight));
                                    graphicsOriginal.ResetClip();
                                }
                            }




                            imageOriginalSize.Save(memoryStream, ImageFormat.Png);
                            return File(memoryStream.ToArray(), "image/png");
                        }
                    }
                }
            }

            return RedirectToAction(nameof(Index));
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

            var requestLicence = await _context.RequestLicenceConcursanteSports.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaNacionalConcursante", "LCF.jpg");

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

        public async Task<IActionResult> CreateImageInternacionalAnverso(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestLicenceConcursanteSports.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }

            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaInternacionalConcursante", "Licencia_Concursante_Internacional_02.png");
            //var fondoImage = Path.Combine("http://forms.automovilclubcr.com/", "images/LicenciaInternacionalConcursante", "Licencia_Concursante_Internacional_02.png");
            var fotoRecienteUrl = requestLicence.PhotoFullPath;

            if (requestLicence.Expedition != null && requestLicence.Expiration != null)
            {
                using (HttpClient client = new HttpClient())
                using (Stream fotoStream = await client.GetStreamAsync(fotoRecienteUrl))
                using (var fondoOriginal = System.Drawing.Image.FromFile(fondoImage))
                using (var foto = System.Drawing.Image.FromStream(fotoStream))
                // Primero creamos la imagen en tamaño original (2048x3125)
                using (var imageOriginalSize = new Bitmap(2084, 3125))
                using (var graphicsOriginal = Graphics.FromImage(imageOriginalSize))
                // Luego creamos la imagen final redimensionada (502x750)
                using (var imageFinal = new Bitmap(502, 750))
                using (var graphicsFinal = Graphics.FromImage(imageFinal))
                using (var memoryStream = new MemoryStream())
                {
                    // Configuración de calidad para ambas operaciones
                    graphicsOriginal.SmoothingMode = graphicsFinal.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphicsOriginal.InterpolationMode = graphicsFinal.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphicsOriginal.PixelOffsetMode = graphicsFinal.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    graphicsOriginal.CompositingQuality = graphicsFinal.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                    // 1. Trabajar en tamaño original (2048x3125)
                    graphicsOriginal.DrawImage(fondoOriginal, 0, 0, 2084, 3125);

                    // ===== FOTO DEL TRAMITANTE (medidas originales) =====
                    System.Drawing.Rectangle photoRect = new System.Drawing.Rectangle(190, 1908, 612, 862);

                    // Bordes redondeados (13px como especificado)
                    int cornerRadius = 13;
                    int borderSize = 2;

                    System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddArc(photoRect.X, photoRect.Y, cornerRadius, cornerRadius, 180, 90);
                    path.AddArc(photoRect.X + photoRect.Width - cornerRadius, photoRect.Y, cornerRadius, cornerRadius, 270, 90);
                    path.AddArc(photoRect.X + photoRect.Width - cornerRadius, photoRect.Y + photoRect.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
                    path.AddArc(photoRect.X, photoRect.Y + photoRect.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
                    path.CloseFigure();

                    using (var fillBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                    {
                        graphicsOriginal.FillPath(fillBrush, path);
                    }

                    using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.White, borderSize))
                    {
                        graphicsOriginal.DrawPath(borderPen, path);
                    }

                    System.Drawing.Rectangle photoRectWithBorder = System.Drawing.Rectangle.Inflate(photoRect, -borderSize, -borderSize);
                    graphicsOriginal.SetClip(path);
                    graphicsOriginal.DrawImage(foto, photoRectWithBorder);
                    graphicsOriginal.ResetClip();

                    // ===== TEXTOS (medidas originales) =====
                    string nombreCompleto = !string.IsNullOrEmpty(requestLicence.ShortName) ? requestLicence.ShortName : requestLicence.Name;
                    string numeroLicencia = requestLicence.LicenceNumber;

                    // Fuentes con tamaños originales (29pt para nombre, 17pt para datos)
                    using (var fontName = _fontsHelper.GetFont("Visby CF Extra Bold", 92, System.Drawing.FontStyle.Bold))
                    using (var fontData = _fontsHelper.GetFont("Visby CF Extra Bold", 62, System.Drawing.FontStyle.Bold))
                    using (var brushWhite = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 255, 255)))
                    {
                        // ===== NOMBRE CENTRADO =====
                        var nameSize = graphicsOriginal.MeasureString(nombreCompleto, fontName);
                        float nameX = (2084 - nameSize.Width) / 2; // Centrado en 2048px
                        graphicsOriginal.DrawString(nombreCompleto, fontName, brushWhite, nameX, 770);

                        // ===== DATOS ALINEADOS A LA DERECHA =====
                        float rightMargin = 980 - 10; // 30px del borde derecho
                        float currentY = 1065;

                        // Número de licencia (incluye etiqueta)
                        string licenceText = $"{numeroLicencia}";
                        var licenceSize = graphicsOriginal.MeasureString(licenceText, fontData);
                        graphicsOriginal.DrawString(licenceText, fontData, brushWhite, rightMargin, currentY);
                        currentY += 142;

                        // Fecha de nacimiento
                        string birthText = $"{requestLicence.BirthDay.Value.ToString("d/M/yyyy")}";
                        var birthSize = graphicsOriginal.MeasureString(birthText, fontData);
                        graphicsOriginal.DrawString(birthText, fontData, brushWhite, rightMargin, currentY);
                        currentY += 146;

                        // Fecha de expedición
                        string expeditionText = $"{requestLicence.Expedition.Value.ToString("d/M/yyyy")}";
                        var expeditionSize = graphicsOriginal.MeasureString(expeditionText, fontData);
                        graphicsOriginal.DrawString(expeditionText, fontData, brushWhite, rightMargin, currentY);
                        currentY += 147;

                        // Fecha de vencimiento
                        string expirationText = $"{requestLicence.Expiration.Value.ToString("d/M/yyyy")}";
                        var expirationSize = graphicsOriginal.MeasureString(expirationText, fontData);
                        graphicsOriginal.DrawString(expirationText, fontData, brushWhite, rightMargin, currentY);
                    }

                    // ===== QR CODE (medidas originales) =====
                    // int qrSize = 93; // Tamaño original
                    // Rectangle qrRect = new Rectangle(x, y, qrSize, qrSize);
                    // Implementar generación y dibujo del QR aquí

                    string datosCompletos = $"License approved and authorized by the Automóbil Club de Costa Rica.";


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
                        int qrX = 1310; // Posición en el eje X
                        int qrY = 2100; // Posición en el eje Y
                        int qrCornerRadius = 15;

                        using (GraphicsPath qrPath = new GraphicsPath())
                        {
                            qrPath.AddArc(qrX, qrY, qrCornerRadius, qrCornerRadius, 180, 90);
                            qrPath.AddArc(qrX + qrWidth - qrCornerRadius, qrY, qrCornerRadius, qrCornerRadius, 270, 90);
                            qrPath.AddArc(qrX + qrWidth - qrCornerRadius, qrY + qrHeight - qrCornerRadius, qrCornerRadius, qrCornerRadius, 0, 90);
                            qrPath.AddArc(qrX, qrY + qrHeight - qrCornerRadius, qrCornerRadius, qrCornerRadius, 90, 90);
                            qrPath.CloseFigure();

                            graphicsOriginal.SetClip(qrPath);
                            graphicsOriginal.DrawImage(qrImage, new System.Drawing.Rectangle(qrX, qrY, qrWidth, qrHeight));
                            graphicsOriginal.ResetClip();
                        }
                    }


                    // 2. Redimensionar a 502x750 con alta calidad
                    //graphicsFinal.DrawImage(imageOriginalSize, 0, 0, 502, 750);

                    imageOriginalSize.Save(memoryStream, ImageFormat.Png);
                    return File(memoryStream.ToArray(), "image/png");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // Método para dividir el nombre en dos líneas si es necesario
        private static (string firstLine, string secondLine) SplitName(string fullName, int maxWidth, Graphics graphics, System.Drawing.Font font)
        {
            if (graphics.MeasureString(fullName, font).Width <= maxWidth)
            {
                return (fullName, string.Empty);
            }

            var names = fullName.Split(' ');
            var firstLine = names[0];
            var secondLine = string.Join(" ", names.Skip(1));

            while (graphics.MeasureString(firstLine + " " + secondLine, font).Width > maxWidth && secondLine.Contains(' '))
            {
                var splitIndex = secondLine.LastIndexOf(' ');
                firstLine += " " + secondLine.Substring(0, splitIndex);
                secondLine = secondLine.Substring(splitIndex + 1);
            }

            return (firstLine, secondLine);
        }



        public async Task<IActionResult> CreateImageInternacionalReverso(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicence = await _context.RequestLicenceConcursanteSports.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaInternacionalConcursante", "LICF.jpg");

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
        private bool ContienePalabra(string texto, string palabra)
        {
            // Puedes implementar tu lógica de búsqueda aquí, por ejemplo, utilizando expresiones regulares.
            return texto.Contains(palabra, StringComparison.OrdinalIgnoreCase);
        }

        //public async Task<IActionResult> UpdateFullApproved(int? id, int? filterTypeId, int? filterLicenceType)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var requestLicenceSportInternational = await _context.RequestLicenceConcursanteSports.FindAsync(id);
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


        //        _mailHelper.SendMail(requestLicenceSportInternational.Mail, "Aprobación de Solicitud de licencia deportiva",
        //            _configurationApp.MessageSolicitudApproved + GenerateHtmlTable(requestLicenceSportInternational, 1));

        //        _mailHelper.SendMail(_configuration["Mail:Admin"], "Aprobación de Solicitud de licencia deportiva",
        //            _configurationApp.MessageSolicitudAdminAprobado + GenerateHtmlTable(requestLicenceSportInternational, 1));
        //    }
        //    else
        //    {
        //        requestLicenceSportInternational.Approved = null;
        //    }

        //    requestLicenceSportInternational.Modify = DateTime.Now;

        //    _context.Update(requestLicenceSportInternational);
        //    await _context.SaveChangesAsync();

        //    return RedirectToAction(nameof(Index), new { filterTypeId, filterLicenceType });
        //}

        public async Task<IActionResult> UpdateFullRejection(int? id, string? motive, int? filterTypeId, int? filterLicenceType)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSportInternational = await _context.RequestLicenceConcursanteSports.FindAsync(id);
            if (requestLicenceSportInternational == null)
            {
                return NotFound();
            }

            requestLicenceSportInternational.FullRejection = !requestLicenceSportInternational.FullRejection;

            if (requestLicenceSportInternational.FullRejection)
            {
                requestLicenceSportInternational.Rejection = DateTime.Now;
                requestLicenceSportInternational.Approved = null;
                requestLicenceSportInternational.FullApproved = false;

                _context.Add(new Rejection()
                {
                    Create = DateTime.Now,
                    Motive = motive,
                    RequestLicenceConcursanteSportId = requestLicenceSportInternational.RequestLicenceConcursanteSportId                 
                });

                await _context.SaveChangesAsync();

                _mailHelper.SendMail(requestLicenceSportInternational.Mail, "Rechazo de Solicitud de permiso internacional",
                    _configurationApp.MessageSolicitudRejection + GenerateHtmlTable(requestLicenceSportInternational, 2) + "<br /> <b>Motivo:</b> <br/>" + motive);

                _mailHelper.SendMail(_configuration["Mail:Admin"], "Rechazo de Solicitud de permiso internacional",
                    _configurationApp.MessageSolicitudAdminRejection + GenerateHtmlTable(requestLicenceSportInternational, 2) 
                    + "<br /> <b>Motivo:</b> <br/>" + motive);
            }
            else
            {
                requestLicenceSportInternational.Rejection = null;
            }

            requestLicenceSportInternational.Modify = DateTime.Now;

            _context.Update(requestLicenceSportInternational);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { filterTypeId, filterLicenceType });
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



        public async Task<IActionResult> EditPartial(int? id, int? filterTypeId, int? filterLicenceType)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSport = await _context.RequestLicenceConcursanteSports.FindAsync(id);
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


            var model = new RequestLicenceConcursanteSportViewModelShort()
            {
                Expedition = requestLicenceSport.Expedition,
                Expiration = requestLicenceSport.Expiration,
                LicenceType = requestLicenceSport.LicenceType,
                ShortName = requestLicenceSport.ShortName,
                RequestLicenceConcursanteSportId=requestLicenceSport.RequestLicenceConcursanteSportId,
                LicenceNumber= requestLicenceSport.LicenceNumber,
                FilterLicenceType = filterLicenceType,
                FilterTypeId = filterTypeId,
                Photo=requestLicenceSport.Photo,
            };

            if (model.ShortName == null)
                model.ShortName = requestLicenceSport.Name;

            return View("_EditPartial", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPartial(RequestLicenceConcursanteSportViewModelShort model)
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
                try
                {
                    var pic = model.Photo;
                    var guid = Guid.NewGuid();
                    var nombreArchivoPhoto = "PH_" + guid + ".jpg";


                    var requestLicenceSport = await _context.RequestLicenceConcursanteSports.FindAsync(model.RequestLicenceConcursanteSportId);

                    requestLicenceSport.Expedition = model.Expedition;
                    requestLicenceSport.Expiration = model.Expiration;
                    requestLicenceSport.LicenceType = model.LicenceType;
                    requestLicenceSport.RequestLicenceConcursanteSportId = model.RequestLicenceConcursanteSportId;
                    requestLicenceSport.ShortName = model.ShortName;
                    requestLicenceSport.LicenceNumber = model.LicenceNumber;
                    requestLicenceSport.Photo = model.Photo;


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
                    if (!RequestLicenceSportExists(model.RequestLicenceConcursanteSportId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
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



        public async Task<IActionResult> CreateImageAnverso2(int? id)
        {
   

            var requestLicence = await _context.RequestLicenceConcursanteSports.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }

            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaNacionalConcursante", "LICENCIA-NACIONAL-CONCURSANTE2.jpg");
            var fotoRecienteUrl = requestLicence.PhotoFullPath;
            var pathLogo1 = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaNacionalConcursante", "ICONO2.png");

            using (var logo1 = System.Drawing.Image.FromFile(pathLogo1))
            using (HttpClient client = new HttpClient())
            using (Stream fotoStream = await client.GetStreamAsync(fotoRecienteUrl))
            using (var fondo = System.Drawing.Image.FromFile(fondoImage))
            using (var foto = System.Drawing.Image.FromStream(fotoStream))
            using (var image = new Bitmap(fondo.Width, fondo.Height))
            using (var graphics = Graphics.FromImage(image))
            using (var memoryStream = new MemoryStream())
            {
                graphics.DrawImage(fondo, 0, 0);

                System.Drawing.Rectangle photoRect = new System.Drawing.Rectangle(170, 470, 160, 218);
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

                string nombreCompleto = !string.IsNullOrEmpty(requestLicence.ShortName) ? requestLicence.ShortName : requestLicence.Name;
                string numeroLicencia = requestLicence.LicenceNumber;

                using (var fontTitle = _fontsHelper.GetFont("OPTIEdgar-Extended", 18, System.Drawing.FontStyle.Bold))
                using (var fontLabel = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold))
                using (var fontBodySmall = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
                using (var fontBody = _fontsHelper.GetFont("Visby CF Extra Bold", 18, System.Drawing.FontStyle.Bold))
                using (var fontBody2 = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
                using (var fontEquis = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Bold))
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                using (var brushBlack = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2))
                {
                    var imageWidth = fondo.Width;
                    var availableWidth = imageWidth - 200; // Espacio disponible considerando un margen de 30 píxeles a cada lado

                    // Divide el nombre en dos líneas si es necesario
                    var (firstLine, secondLine) = StringExtensions.SplitName(nombreCompleto, availableWidth, graphics, fontTitle);

                    var firstLineSize = graphics.MeasureString(firstLine, fontTitle);
                    var secondLineSize = graphics.MeasureString(secondLine, fontTitle);
                    var licenceNumberSize = graphics.MeasureString(numeroLicencia, fontBody);
                    var birthDateSize = graphics.MeasureString(requestLicence.BirthDay.Value.ToString("d/M/yyyy"), fontBody);
                    var expeditionSize = graphics.MeasureString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody);
                    var expirationSize = graphics.MeasureString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody);

                    // Calcular las posiciones para centrar el texto
                    var centerX = imageWidth / 2;

                    // Dibujar el nombre completo centrado

                    //var y1 = 142;
                    //var y2 = 162;

                    graphics.DrawString(firstLine, fontTitle, brush, new System.Drawing.PointF(centerX - firstLineSize.Width / 2, !string.IsNullOrEmpty(secondLine) ? 127 : 142));
                    if (!string.IsNullOrEmpty(secondLine))
                    {
                        graphics.DrawString(secondLine, fontTitle, brush, new System.Drawing.PointF(centerX - secondLineSize.Width / 2, 157));
                    }

                    // Dibujar los demás textos alineados a la derecha
                    var rightEdge = imageWidth - 30; // 30 píxeles de margen derecho
                    graphics.DrawString(numeroLicencia, fontBody, brush, new System.Drawing.PointF(rightEdge - licenceNumberSize.Width, 245));
                    graphics.DrawString(requestLicence.BirthDay.Value.ToString("M/d/yyyy"), fontBody, brush, new System.Drawing.PointF(rightEdge - birthDateSize.Width, 290));
                    graphics.DrawString(requestLicence.Expedition.Value.ToString("M/d/yyyy"), fontBody, brush, new System.Drawing.PointF(rightEdge - expeditionSize.Width, 332));
                    graphics.DrawString(requestLicence.Expiration.Value.ToString("M/d/yyyy"), fontBody, brush, new System.Drawing.PointF(rightEdge - expirationSize.Width, 378));
                }

                image.Save(memoryStream, ImageFormat.Png);
                return File(memoryStream.ToArray(), "image/png");
            }
        }

        public async Task<IActionResult> CreateImageAnverso3(int? id)
        {


            var requestLicence = await _context.RequestLicenceConcursanteSports.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }

            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaNacionalConcursante", "Licencia_Concursante_Nacional_02.png");
            var fotoRecienteUrl = requestLicence.PhotoFullPath;
            //var pathLogo1 = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaNacionalConcursante", "ICONO2.png");

            //using (var logo1 = System.Drawing.Image.FromFile(pathLogo1))
            using (HttpClient client = new HttpClient())
            using (Stream fotoStream = await client.GetStreamAsync(fotoRecienteUrl))
            using (var fondo = System.Drawing.Image.FromFile(fondoImage))
            using (var foto = System.Drawing.Image.FromStream(fotoStream))
            // Primero creamos la imagen en tamaño original (2048x3125)
            using (var imageOriginalSize = new Bitmap(2084, 3125))
            using (var graphics = Graphics.FromImage(imageOriginalSize))
            // Luego creamos la imagen final redimensionada (502x750)
            using (var imageFinal = new Bitmap(2084, 3125))
            using (var graphicsFinal = Graphics.FromImage(imageFinal))
            using (var memoryStream = new MemoryStream())
            {
                graphics.DrawImage(fondo, 0, 0);

                System.Drawing.Rectangle photoRect = new System.Drawing.Rectangle(170, 470, 160, 218);
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

                string nombreCompleto = !string.IsNullOrEmpty(requestLicence.ShortName) ? requestLicence.ShortName : requestLicence.Name;
                string numeroLicencia = requestLicence.LicenceNumber;

                using (var fontTitle = _fontsHelper.GetFont("OPTIEdgar-Extended", 18, System.Drawing.FontStyle.Bold))
                using (var fontLabel = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold))
                using (var fontBodySmall = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
                using (var fontBody = _fontsHelper.GetFont("Visby CF Extra Bold", 18, System.Drawing.FontStyle.Bold))
                using (var fontBody2 = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
                using (var fontEquis = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Bold))
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                using (var brushBlack = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2))
                {
                    var imageWidth = fondo.Width;
                    var availableWidth = imageWidth - 200; // Espacio disponible considerando un margen de 30 píxeles a cada lado

                    // Divide el nombre en dos líneas si es necesario
                    var (firstLine, secondLine) = StringExtensions.SplitName(nombreCompleto, availableWidth, graphics, fontTitle);

                    var firstLineSize = graphics.MeasureString(firstLine, fontTitle);
                    var secondLineSize = graphics.MeasureString(secondLine, fontTitle);
                    var licenceNumberSize = graphics.MeasureString(numeroLicencia, fontBody);
                    var birthDateSize = graphics.MeasureString(requestLicence.BirthDay.Value.ToString("d/M/yyyy"), fontBody);
                    var expeditionSize = graphics.MeasureString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody);
                    var expirationSize = graphics.MeasureString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody);

                    // Calcular las posiciones para centrar el texto
                    var centerX = imageWidth / 2;

                    // Dibujar el nombre completo centrado

                    //var y1 = 142;
                    //var y2 = 162;

                    graphics.DrawString(firstLine, fontTitle, brush, new System.Drawing.PointF(centerX - firstLineSize.Width / 2, !string.IsNullOrEmpty(secondLine) ? 127 : 142));
                    if (!string.IsNullOrEmpty(secondLine))
                    {
                        graphics.DrawString(secondLine, fontTitle, brush, new System.Drawing.PointF(centerX - secondLineSize.Width / 2, 157));
                    }

                    // Dibujar los demás textos alineados a la derecha
                    var rightEdge = imageWidth - 30; // 30 píxeles de margen derecho
                    graphics.DrawString(numeroLicencia, fontBody, brush, new System.Drawing.PointF(rightEdge - licenceNumberSize.Width, 245));
                    graphics.DrawString(requestLicence.BirthDay.Value.ToString("M/d/yyyy"), fontBody, brush, new System.Drawing.PointF(rightEdge - birthDateSize.Width, 290));
                    graphics.DrawString(requestLicence.Expedition.Value.ToString("M/d/yyyy"), fontBody, brush, new System.Drawing.PointF(rightEdge - expeditionSize.Width, 332));
                    graphics.DrawString(requestLicence.Expiration.Value.ToString("M/d/yyyy"), fontBody, brush, new System.Drawing.PointF(rightEdge - expirationSize.Width, 378));
                }

                imageFinal.Save(memoryStream, ImageFormat.Png);
                return File(memoryStream.ToArray(), "image/png");
            }
        }


        private async Task<IActionResult> GenerateLicenceImage(RequestLicenceConcursanteSport requestLicence, string tipo)
        {
            if (requestLicence.LicenceType == LicenceType.Nacional)
            {
                if (tipo == "anverso")
                {
                    return await CreateImageAnverso(requestLicence.RequestLicenceConcursanteSportId);
                } 
                
            }
            else
            {
                if (tipo == "anverso")
                {
                    return await CreateImageInternacionalAnverso(requestLicence.RequestLicenceConcursanteSportId);
                }                
            }

            return null;
        }


        private async Task<IActionResult> GenerateLicenceImageBK(RequestLicenceConcursanteSport requestLicence, string tipo)
        {
            if (requestLicence.LicenceType == LicenceType.Nacional)
            {
                if (tipo == "anverso")
                {
                    return await CreateImageAnverso3(requestLicence.RequestLicenceConcursanteSportId);
                }
                else
                {
                    return await CreateImageReverso(requestLicence.RequestLicenceConcursanteSportId);
                }
            }
            else 
            {
                if (tipo == "anverso")
                {
                    return await CreateImageInternacionalAnverso(requestLicence.RequestLicenceConcursanteSportId);
                }
                else
                {
                    return await CreateImageInternacionalReverso(requestLicence.RequestLicenceConcursanteSportId);
                }
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

        public async Task<IActionResult> UpdateFullApproved(int? id, int? filterTypeId, int? filterLicenceType)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestLicenceSport = await _context.RequestLicenceConcursanteSports.FindAsync(id);
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

                string anversoPath = SaveImageFromResult(anversoResult, "anverso", requestLicenceSport.RequestLicenceConcursanteSportId);
                //string reversoPath = SaveImageFromResult(reversoResult, "reverso", requestLicenceSport.RequestLicenceConcursanteSportId);

                var anversoContent = System.IO.File.ReadAllBytes(anversoPath);
                //var reversoContent = System.IO.File.ReadAllBytes(reversoPath);

                var attachments = new List<(string FileName, byte[] Content)>
                {
                         ($"Licencia de Concursante {(requestLicenceSport.LicenceType == LicenceType.Nacional ? "Nacional" : "Internacional")}.png", anversoContent)

                    //("Reverso.png", reversoContent)
                };

                _mailHelper.SendMail(
                    requestLicenceSport.Mail,
                    "Aprobación de Solicitud de licencia de concursante",
                    _configurationApp.MessageSolicitudApproved + GenerateHtmlTable(requestLicenceSport, 1),
                    attachments
                );

                _mailHelper.SendMail(
                    _configuration["Mail:Admin"],
                    "Aprobación de Solicitud de licencia de concursante",
                    _configurationApp.MessageSolicitudAdminAprobado + GenerateHtmlTable(requestLicenceSport, 1)
                );
            }
            else
            {
                requestLicenceSport.Approved = null;
            }

            requestLicenceSport.Modify = DateTime.Now;

            _context.Update(requestLicenceSport);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { filterTypeId, filterLicenceType });
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
