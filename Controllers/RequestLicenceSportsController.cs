using AutomovilClub.Backend.Data;
using AutomovilClub.Backend.Data.Entities;
using AutomovilClub.Backend.Enums;
using AutomovilClub.Backend.Helpers;
using AutomovilClub.Backend.Models;
using IronOcr;
using IronSoftware.Drawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QRCoder;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography.X509Certificates;
using static NuGet.Packaging.PackagingConstants;



namespace AutomovilClub.Backend.Controllers
{
    public class RequestLicenceSportsController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMailHelper _mailHelper;
        private readonly IConfiguration _configuration;
        private readonly IFontsHelper _fontsHelper;
        private readonly Data.Entities.Configuration _configurationApp;

        public RequestLicenceSportsController(DataContext context, 
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


        public async Task<IActionResult> Index(int? filterTypeId = 0, int? filterLicenceType=2, string? userFilterId="-1")
        {
            var requestLicenceSports = await _context.RequestLicenceSports.Include(r => r.Country).Include(u=>u.User).ToListAsync();

            ViewData["SelectedUserId"] = userFilterId;

            if (userFilterId != null && userFilterId!="-1")
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
                 .Concat(_context.Users
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
            ViewData["CountryId"] = new SelectList(_context.Countries, "CountryId", "Name");
            return View(new RequestLicenceSportViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RequestLicenceSportViewModel model)
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
                ViewData["CountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.CountryId);
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
                ViewData["CountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.CountryId);
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var guid = Guid.NewGuid();
                var nombreArchivoMedicalExam = "EM_" + guid + ".pdf";
                var nombreArchivoElectrocardiogram = "ECG_" + guid + ".pdf";
                var nombreArchivoPhoto = "PH_" + guid + ".jpg";
                var nombreCertificate = "CC_" + guid + ".pdf";

                // Lógica para leer y validar documentos PDF
                if (model.MedicalExamFile != null)
                {   
                    // Guardar el archivo en el servidor
                    var pdfPathMedicalExam = await GuardarArchivoEnServidor(model.MedicalExamFile, @"files\pdf\" + nombreArchivoMedicalExam);
                    var pdfPathElectrocardiogram = await GuardarArchivoEnServidor(model.ElectrocardiogramFile, @"files\pdf\" + nombreArchivoElectrocardiogram);
                    var imgPathPhoto= await GuardarArchivoEnServidor(model.PhotoFile, @"files\photos\" + nombreArchivoPhoto);

                    var imgPathCertificatevar = "";
                    if (model.CourseCertificateFile != null) 
                    {
                        imgPathCertificatevar = await GuardarArchivoEnServidor(model.CourseCertificateFile, @"files\pdf\" + nombreCertificate);
                    }

                    model.Photo= imgPathPhoto;

                    if (_configurationApp.ValidateMedicalExam) 
                    {
                        var textoMedicalExam = await EjemploLecturaTextoEnImagenAsync(pdfPathMedicalExam, guid.ToString());
                        bool aproveMedicalExam = ContienePalabra(textoMedicalExam, "EXAMEN MEDICO DE APTITUD\r\nPARA LA OBTENCION DE\r\nLA LICENCIA DEPORTIVA");

                        if (aproveMedicalExam)
                        {
                            model.MedicalExam = pdfPathMedicalExam;
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "El examen médico no es válido");
                            ViewData["CountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.CountryId);
                            return View(model);
                        }
                    }

                    if (_configurationApp.ValidateElectrocardiogram)
                    {
                        var textoElectrocardiogram = await EjemploLecturaTextoEnImagenAsync(pdfPathElectrocardiogram, guid.ToString());
                        bool aproveElectrocardiogram = ContienePalabra(textoElectrocardiogram, "RITMO SINUSAL");

                        if (aproveElectrocardiogram)
                        {
                            model.Electrocardiogram = pdfPathElectrocardiogram;
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "El electrocardiograma no es válido");
                            ViewData["CountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.CountryId);
                            return View(model);
                        }
                    }
                }

                var requestLicenceSport = new RequestLicenceSport()
                {
                    CountryId = model.CountryId,
                    Identification = model.Identification,
                    Name = model.Name,
                    Mail = model.Mail,
                    PhoneNumber = model.PhoneNumber,
                    Photo = model.PhotoFile != null ? $"~/files/photos/{nombreArchivoPhoto}" : null,
                    MedicalExam = model.MedicalExamFile != null ? $"~/files/pdf/{nombreArchivoMedicalExam}" : null,
                    Electrocardiogram = model.ElectrocardiogramFile != null ? $"~/files/pdf/{nombreArchivoElectrocardiogram}" : null,                    
                    LicenceNumber =  model.LicenceNumber,
                    BirthDay=model.BirthDay,
                    LicenceType=model.LicenceType
                };

                if (nombreCertificate != "") 
                {
                    requestLicenceSport.CourseCertificate =model.CourseCertificateFile !=null ? $"~/files/pdf/{nombreCertificate}":null;
                }

                _context.Add(requestLicenceSport);
                await _context.SaveChangesAsync();

                _mailHelper.SendMail(model.Mail, "Solicitud de licencia deportiva", 
                    _configurationApp.MessageSolicitud + GenerateHtmlTable(model, 0));

                _mailHelper.SendMail(_configuration["Mail:Admin"], "Solicitud de licencia deportiva", 
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
            ViewData["CountryId"] = new SelectList(_context.Countries, "CountryId", "Name", model.CountryId);
            return View(model);
        }

        public string GenerateHtmlTable(RequestLicenceSportViewModel model, int estado)
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

        public string GenerateHtmlTable(RequestLicenceSport model, int estado)
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




        //public async Task<IActionResult> CreateImageAnverso(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var requestLicence = await _context.RequestLicenceSports.FindAsync(id);
        //    if (requestLicence == null)
        //    {
        //        return NotFound();
        //    }

        //    var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaNacional", "Licencia_Deportiva_Nacional_02.png");
        //    var fotoRecienteUrl = requestLicence.PhotoFullPath;

        //    if (requestLicence.Expedition != null && requestLicence.Expiration != null && requestLicence.FiaMS != null)
        //    {
        //        using (HttpClient client = new HttpClient())
        //        {
        //            var response = await client.GetAsync(fotoRecienteUrl);

        //            if (!response.IsSuccessStatusCode)
        //                throw new Exception($"No se pudo descargar la imagen: {response.StatusCode}");

        //            using (var fotoStream = await response.Content.ReadAsStreamAsync())
        //            using (var memoryStreamFoto = new MemoryStream())
        //            {
        //                await fotoStream.CopyToAsync(memoryStreamFoto);
        //                memoryStreamFoto.Position = 0;

        //                using (var fondo = System.Drawing.Image.FromFile(fondoImage))
        //                using (var foto = System.Drawing.Image.FromStream(memoryStreamFoto))
        //                    graphics.DrawImage(fondo, 0, 0, 2084, 3125);

        //            graphicsOriginal.SmoothingMode = graphicsFinal.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        //            graphicsOriginal.InterpolationMode = graphicsFinal.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        //            graphicsOriginal.PixelOffsetMode = graphicsFinal.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        //            graphicsOriginal.CompositingQuality = graphicsFinal.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

        //            // 1. Trabajar en tamaño original (2048x3125)
        //            graphicsOriginal.DrawImage(fondo, 0, 0, 2084, 3125);

        //            System.Drawing.Rectangle photoRect = new System.Drawing.Rectangle(790, 485, 500, 690);
        //            int cornerRadius = 20;
        //            int borderSize = 5;

        //            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
        //            path.AddArc(photoRect.X, photoRect.Y, cornerRadius, cornerRadius, 180, 90);
        //            path.AddArc(photoRect.X + photoRect.Width - cornerRadius, photoRect.Y, cornerRadius, cornerRadius, 270, 90);
        //            path.AddArc(photoRect.X + photoRect.Width - cornerRadius, photoRect.Y + photoRect.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
        //            path.AddArc(photoRect.X, photoRect.Y + photoRect.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
        //            path.CloseFigure();

        //            using (var fillBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
        //            {
        //                graphics.FillPath(fillBrush, path);
        //            }

        //            using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.White, borderSize))
        //            {
        //                graphics.DrawPath(borderPen, path);
        //            }

        //            System.Drawing.Rectangle photoRectWithBorder = System.Drawing.Rectangle.Inflate(photoRect, -borderSize, -borderSize);
        //            graphics.DrawImage(foto, photoRectWithBorder);
        //            graphics.ResetClip();

        //            string nombreCompleto = !string.IsNullOrEmpty(requestLicence.ShortName) ? requestLicence.ShortName : requestLicence.Name;
        //            string numeroLicencia = requestLicence.LicenceNumber;

        //            using (var fontTitle = _fontsHelper.GetFont("OPTIEdgar-Extended", 92, System.Drawing.FontStyle.Bold))
        //            using (var fontName = _fontsHelper.GetFont("Visby CF Extra Bold", 92, System.Drawing.FontStyle.Bold))
        //            using (var fontData = _fontsHelper.GetFont("Visby CF Extra Bold", 62, System.Drawing.FontStyle.Bold))
        //            using (var fontLabel = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold))
        //            using (var fontBodySmall = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
        //            using (var fontBody = _fontsHelper.GetFont("Visby CF Extra Bold", 62, System.Drawing.FontStyle.Bold))
        //            using (var fontBody2 = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
        //            using (var fontEquis = new System.Drawing.Font("Arial", 64, System.Drawing.FontStyle.Bold))
        //            using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
        //            using (var brushBlack = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
        //            using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2))
        //            {
        //                var imageWidth = fondo.Width;
        //                var availableWidth = imageWidth - 30; // Espacio disponible considerando un margen de 30 píxeles a cada lado

        //                // Divide el nombre en dos líneas si es necesario
        //                var (firstLine, secondLine) = SplitName(nombreCompleto, availableWidth, graphics, fontTitle);

        //                var firstLineSize = graphics.MeasureString(firstLine, fontTitle);
        //                var nombreCompletoSize = graphics.MeasureString(nombreCompleto, fontTitle);
        //                var secondLineSize = graphics.MeasureString(secondLine, fontTitle);
        //                var licenceNumberSize = graphics.MeasureString(numeroLicencia, fontBody);
        //                var gradeSize = graphics.MeasureString(requestLicence.Grade, fontBody);
        //                var birthDateSize = graphics.MeasureString(requestLicence.BirthDay.Value.ToString("d/M/yyyy"), fontBody);
        //                var expeditionSize = graphics.MeasureString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody);
        //                var expirationSize = graphics.MeasureString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody);
        //                var fiaMSSize = graphics.MeasureString(requestLicence.FiaMS.Value.ToString("d/M/yyyy"), fontBody);


        //                // Dibujar los demás textos alineados a la derecha
        //                var rightEdge = 920; // 30 píxeles de margen derecho

        //                // Calcular las posiciones para centrar el texto
        //                var centerX = imageWidth / 2;

        //                // Dibujar el nombre completo centrado
        //                //graphics.DrawString(firstLine, fontTitle, brush, new System.Drawing.PointF(centerX - firstLineSize.Width / 2, 980));
        //                var nameSize = graphicsOriginal.MeasureString(nombreCompleto, fontName);
        //                float nameX = (2048 - nameSize.Width) / 2;
        //                graphics.DrawString(nombreCompleto, fontName, brush, nameX, 1230);
        //                //if (!string.IsNullOrEmpty(secondLine))
        //                //{
        //                //    graphics.DrawString(secondLine, fontTitle, brush, new System.Drawing.PointF(centerX - secondLineSize.Width / 2, 355));
        //                //}                                                
        //                float currentY = 1500;
        //                if (!string.IsNullOrEmpty(numeroLicencia))
        //                    graphics.DrawString(numeroLicencia, fontBody, brush, rightEdge, currentY);

        //                currentY += 142;


        //                if (!string.IsNullOrEmpty(requestLicence.Grade))
        //                    graphics.DrawString(requestLicence.Grade, fontBody, brush, rightEdge, currentY);

        //                currentY += 144;

        //                if (requestLicence.BirthDay != null)
        //                    graphics.DrawString(requestLicence.BirthDay.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);

        //                currentY += 142;


        //                if (requestLicence.Expedition != null)
        //                    graphics.DrawString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);

        //                currentY += 142;


        //                if (requestLicence.Expiration != null)
        //                    graphics.DrawString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);

        //                currentY += 690;


        //                if (requestLicence.FiaMS != null)
        //                    graphics.DrawString(requestLicence.FiaMS.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);


        //                int checkboxSize = 82;
        //                System.Drawing.Point checkboxSiPosition = new System.Drawing.Point(940, 2330);
        //                System.Drawing.Point checkboxNoPosition = new System.Drawing.Point(1125, 2330);

        //                System.Drawing.Point checkboxSiPosition2 = new System.Drawing.Point(940, 2472);
        //                System.Drawing.Point checkboxNoPosition2 = new System.Drawing.Point(1125, 2472);

        //                System.Drawing.Point checkboxSiPosition3 = new System.Drawing.Point(940, 2609);
        //                System.Drawing.Point checkboxNoPosition3 = new System.Drawing.Point(1125, 2609);


        //                DrawCheckbox(graphics, pen, brushBlack, fontEquis, checkboxSiPosition, checkboxSize, requestLicence.VistaCorregida);
        //                DrawCheckbox(graphics, pen, brushBlack, fontEquis, checkboxNoPosition, checkboxSize, !requestLicence.VistaCorregida);

        //                DrawCheckbox(graphics, pen, brushBlack, fontEquis, checkboxSiPosition2, checkboxSize, requestLicence.SupervisionMedica);
        //                DrawCheckbox(graphics, pen, brushBlack, fontEquis, checkboxNoPosition2, checkboxSize, !requestLicence.SupervisionMedica);

        //                DrawCheckbox(graphics, pen, brushBlack, fontEquis, checkboxSiPosition3, checkboxSize, requestLicence.ConsentimientoWADB);
        //                DrawCheckbox(graphics, pen, brushBlack, fontEquis, checkboxNoPosition3, checkboxSize, !requestLicence.ConsentimientoWADB);




        //                // Genera el código QR con los datos deseados
        //                //string datosCompletos =
        //                //    $"Nombre Completo: {requestLicence.Name}\n" +
        //                //    $"N. LICENCIA: {requestLicence.LicenceNumber}\n" +
        //                //    $"GRADO: {requestLicence.Grade}\n" +
        //                //    $"FECHA DE NACIMIENTO: {requestLicence.BirthDay.Value.ToString("d/M/yyyy")}\n" +
        //                //    $"EXPEDICIÓN: {requestLicence.Expedition.Value.ToString("d/M/yyyy")}\n" +
        //                //    $"VENCIMIENTO: {requestLicence.Expiration.Value.ToString("d/M/yyyy")}\n" +
        //                //    $"VISTA CORREGIDA: {(requestLicence.VistaCorregida ? "SI" : "NO")}\n" +
        //                //    $"SUPERVISIÓN MÉDICA: {(requestLicence.SupervisionMedica ? "SI" : "NO")}\n" +
        //                //    $"CONSENTIMIENTO PARA EL PROCESAMIENTO DE DATOS PERSONALES EN LA WADB: {(requestLicence.ConsentimientoWADB ? "SI" : "NO")}\n" +
        //                //    $"FIA M.S: {DateTime.Now.Date.ToString("d/M/yyyy")}";

        //                string datosCompletos = $"Licencia aprobada y autorizada por el Automóvil Club de Costa Rica.";                            


        //                QRCodeGenerator qrGenerator = new QRCodeGenerator();
        //                QRCodeData qrCodeData = qrGenerator.CreateQrCode(datosCompletos, QRCodeGenerator.ECCLevel.Q);
        //                var qrCode = new PngByteQRCode(qrCodeData);

        //                byte[] qrCodeImageBytes = qrCode.GetGraphic(
        //                    pixelsPerModule: 20,
        //                    darkColorRgba: new byte[] { 255, 255, 255, 255 }, // módulos blancos
        //                    lightColorRgba: new byte[] { 0, 0, 0, 0 }, // fondo transparente
        //                    drawQuietZones: true
        //                );

        //                // Convertir el array de bytes a un objeto Image
        //                using (MemoryStream ms = new MemoryStream(qrCodeImageBytes))
        //                {
        //                    System.Drawing.Image qrImage = System.Drawing.Image.FromStream(ms);

        //                    // Redondear los bordes del código QR
        //                    int qrWidth = 410;
        //                    int qrHeight = 410;
        //                    int qrX = 1490; // Posición en el eje X
        //                    int qrY = 2305; // Posición en el eje Y
        //                    int qrCornerRadius = 15;

        //                    using (GraphicsPath qrPath = new GraphicsPath())
        //                    {
        //                        qrPath.AddArc(qrX, qrY, qrCornerRadius, qrCornerRadius, 180, 90);
        //                        qrPath.AddArc(qrX + qrWidth - qrCornerRadius, qrY, qrCornerRadius, qrCornerRadius, 270, 90);
        //                        qrPath.AddArc(qrX + qrWidth - qrCornerRadius, qrY + qrHeight - qrCornerRadius, qrCornerRadius, qrCornerRadius, 0, 90);
        //                        qrPath.AddArc(qrX, qrY + qrHeight - qrCornerRadius, qrCornerRadius, qrCornerRadius, 90, 90);
        //                        qrPath.CloseFigure();

        //                        graphics.SetClip(qrPath);
        //                        graphics.DrawImage(qrImage, new System.Drawing.Rectangle(qrX, qrY, qrWidth, qrHeight));
        //                        graphics.ResetClip();
        //                    }
        //                }
        //            }

        //            image.Save(memoryStream, ImageFormat.Png);
        //            return File(memoryStream.ToArray(), "image/png");
        //        }
        //    }

        //    return RedirectToAction(nameof(Index));
        //}

         public async Task<IActionResult> CreateImageAnverso(int? id)
        {
            if (id == null)
                return NotFound();

            var requestLicence = await _context.RequestLicenceSports.FindAsync(id);
            if (requestLicence == null)
                return NotFound();

            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaNacional", "Licencia_Deportiva_Nacional_02.png");
            var fotoRecienteUrl = requestLicence.PhotoFullPath;
            string nombreCompleto = !string.IsNullOrEmpty(requestLicence.ShortName) ? requestLicence.ShortName : requestLicence.Name;
            string numeroLicencia = requestLicence.LicenceNumber;


            if (requestLicence.Expedition != null && requestLicence.Expiration != null && requestLicence.FiaMS != null)
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

                        using (var fontTitle = _fontsHelper.GetFont("OPTIEdgar-Extended", 92, System.Drawing.FontStyle.Bold))
                        using (var fontName = _fontsHelper.GetFont("Visby CF Extra Bold", 92, System.Drawing.FontStyle.Bold))
                        using (var fontData = _fontsHelper.GetFont("Visby CF Extra Bold", 62, System.Drawing.FontStyle.Bold))
                        using (var fontLabel = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold))
                        using (var fontBodySmall = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular))
                        using (var fontBody = _fontsHelper.GetFont("Visby CF Extra Bold", 62, System.Drawing.FontStyle.Bold))
                        using (var fontEquis = new System.Drawing.Font("Arial", 64, System.Drawing.FontStyle.Bold))
                        using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                        using (var brushBlack = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2))
                        using (var fondo = System.Drawing.Image.FromFile(fondoImage))
                        using (var foto = System.Drawing.Image.FromStream(memoryStreamFoto))
                        using (var image = new Bitmap(fondo.Width, fondo.Height))
                        using (var imageOriginalSize = new Bitmap(2084, 3125))
                        using (var graphicsOriginal = Graphics.FromImage(imageOriginalSize))
                        using (var graphics = Graphics.FromImage(image))
                        using (var imageFinal = new Bitmap(2084, 3125))
                        using (var graphicsFinal = Graphics.FromImage(imageFinal))
                        using (var memoryStream = new MemoryStream())
                        {
                            // 💡 Desde aquí continúa tu lógica tal como la tenías
                            graphics.DrawImage(fondo, 0, 0, 2084, 3125);
                            graphicsOriginal.SmoothingMode = graphicsFinal.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                            graphicsOriginal.InterpolationMode = graphicsFinal.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            graphicsOriginal.PixelOffsetMode = graphicsFinal.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            graphicsOriginal.CompositingQuality = graphicsFinal.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;


                            var imageWidth = fondo.Width;
                            var availableWidth = imageWidth - 30; // Espacio disponible considerando un margen de 30 píxeles a cada lado


                            System.Drawing.Rectangle photoRect = new System.Drawing.Rectangle(790, 480, 500, 690);
                            int cornerRadius = 20;
                            int borderSize = 5;

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

                            using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.White, borderSize))
                            {
                                graphics.DrawPath(borderPen, path);
                            }

                            // Ajustar el rectángulo para la foto, considerando el borde
                            System.Drawing.Rectangle photoRectWithBorder = System.Drawing.Rectangle.Inflate(photoRect, -borderSize, -borderSize);

                            graphics.SetClip(path);
                            graphics.DrawImage(foto, photoRectWithBorder);
                            graphics.ResetClip();


                            // Divide el nombre en dos líneas si es necesario
                            var (firstLine, secondLine) = SplitName(nombreCompleto, availableWidth, graphics, fontTitle);

                            var firstLineSize = graphics.MeasureString(firstLine, fontTitle);
                            var nombreCompletoSize = graphics.MeasureString(nombreCompleto, fontTitle);
                            var secondLineSize = graphics.MeasureString(secondLine, fontTitle);
                            var licenceNumberSize = graphics.MeasureString(numeroLicencia, fontBody);
                            var gradeSize = graphics.MeasureString(requestLicence.Grade, fontBody);
                            var birthDateSize = graphics.MeasureString(requestLicence.BirthDay.Value.ToString("d/M/yyyy"), fontBody);
                            var expeditionSize = graphics.MeasureString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody);
                            var expirationSize = graphics.MeasureString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody);
                            var fiaMSSize = graphics.MeasureString(requestLicence.FiaMS.Value.ToString("d/M/yyyy"), fontBody);


                            // Dibujar los demás textos alineados a la derecha
                            var rightEdge = 920; // 30 píxeles de margen derecho

                            // Calcular las posiciones para centrar el texto
                            var centerX = imageWidth / 2;

                            // Dibujar el nombre completo centrado
                            //graphics.DrawString(firstLine, fontTitle, brush, new System.Drawing.PointF(centerX - firstLineSize.Width / 2, 980));
                            var nameSize = graphicsOriginal.MeasureString(nombreCompleto, fontName);
                            float nameX = (2048 - nameSize.Width) / 2;
                            graphics.DrawString(nombreCompleto, fontName, brush, nameX, 1230);
                            //if (!string.IsNullOrEmpty(secondLine))
                            //{
                            //    graphics.DrawString(secondLine, fontTitle, brush, new System.Drawing.PointF(centerX - secondLineSize.Width / 2, 355));
                            //}                                                
                            float currentY = 1500;
                            if (!string.IsNullOrEmpty(numeroLicencia))
                                graphics.DrawString(numeroLicencia, fontBody, brush, rightEdge, currentY);

                            currentY += 142;


                            if (!string.IsNullOrEmpty(requestLicence.Grade))
                                graphics.DrawString(requestLicence.Grade, fontBody, brush, rightEdge, currentY);

                            currentY += 144;

                            if (requestLicence.BirthDay != null)
                                graphics.DrawString(requestLicence.BirthDay.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);

                            currentY += 142;


                            if (requestLicence.Expedition != null)
                                graphics.DrawString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);

                            currentY += 142;


                            if (requestLicence.Expiration != null)
                                graphics.DrawString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);

                            currentY += 690;


                            if (requestLicence.FiaMS != null)
                                graphics.DrawString(requestLicence.FiaMS.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);


                            int checkboxSize = 82;
                            System.Drawing.Point checkboxSiPosition = new System.Drawing.Point(940, 2330);
                            System.Drawing.Point checkboxNoPosition = new System.Drawing.Point(1125, 2330);

                            System.Drawing.Point checkboxSiPosition2 = new System.Drawing.Point(940, 2472);
                            System.Drawing.Point checkboxNoPosition2 = new System.Drawing.Point(1125, 2472);

                            System.Drawing.Point checkboxSiPosition3 = new System.Drawing.Point(940, 2609);
                            System.Drawing.Point checkboxNoPosition3 = new System.Drawing.Point(1125, 2609);


                            DrawCheckbox(graphics, pen, brushBlack, fontEquis, checkboxSiPosition, checkboxSize, requestLicence.VistaCorregida);
                            DrawCheckbox(graphics, pen, brushBlack, fontEquis, checkboxNoPosition, checkboxSize, !requestLicence.VistaCorregida);

                            DrawCheckbox(graphics, pen, brushBlack, fontEquis, checkboxSiPosition2, checkboxSize, requestLicence.SupervisionMedica);
                            DrawCheckbox(graphics, pen, brushBlack, fontEquis, checkboxNoPosition2, checkboxSize, !requestLicence.SupervisionMedica);

                            DrawCheckbox(graphics, pen, brushBlack, fontEquis, checkboxSiPosition3, checkboxSize, requestLicence.ConsentimientoWADB);
                            DrawCheckbox(graphics, pen, brushBlack, fontEquis, checkboxNoPosition3, checkboxSize, !requestLicence.ConsentimientoWADB);


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
                                int qrX = 1490; // Posición en el eje X
                                int qrY = 2305; // Posición en el eje Y
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
                }
            }

            return RedirectToAction(nameof(Index));
        }


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

            var requestLicence = await _context.RequestLicenceSports.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaNacional", "PERMISO-NACIONAL2.jpg");

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

            var requestLicence = await _context.RequestLicenceSports.FindAsync(id);
            if (requestLicence == null)
            {
                return NotFound();
            }
            var fondoImage = Path.Combine(_webHostEnvironment.WebRootPath, "images/LicenciaInternacional", "Licencia_Internacional_01.png");
            var fotoRecienteUrl = requestLicence.PhotoFullPath;

            if (requestLicence.Expedition != null && requestLicence.Expiration != null && requestLicence.FiaMS != null)
            {
                using (HttpClient client = new HttpClient())
                using (Stream fotoStream = await client.GetStreamAsync(fotoRecienteUrl))
                using (var fondoOriginal = System.Drawing.Image.FromFile(fondoImage))
                using (var foto = System.Drawing.Image.FromStream(fotoStream))
                // Primero creamos la imagen en tamaño original (2048x3125)
                using (var imageOriginalSize = new Bitmap(2084, 3125))
                using (var graphicsOriginal = Graphics.FromImage(imageOriginalSize))
                // Luego creamos la imagen final redimensionada (502x750)
                using (var imageFinal = new Bitmap(2084, 3125))
                using (var graphicsFinal = Graphics.FromImage(imageFinal))
                using (var memoryStream = new MemoryStream())
                {

                    graphicsOriginal.SmoothingMode = graphicsFinal.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphicsOriginal.InterpolationMode = graphicsFinal.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphicsOriginal.PixelOffsetMode = graphicsFinal.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    graphicsOriginal.CompositingQuality = graphicsFinal.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                    // 1. Trabajar en tamaño original (2048x3125)
                    graphicsOriginal.DrawImage(fondoOriginal, 0, 0, 2084, 3125);

                    System.Drawing.Rectangle photoRect = new System.Drawing.Rectangle(790, 480, 500, 690);
                    int cornerRadius = 20;
                    int borderSize = 5;

                    System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddArc(photoRect.X, photoRect.Y, cornerRadius, cornerRadius, 180, 90);
                    path.AddArc(photoRect.X + photoRect.Width - cornerRadius, photoRect.Y, cornerRadius, cornerRadius, 270, 90);
                    path.AddArc(photoRect.X + photoRect.Width - cornerRadius, photoRect.Y + photoRect.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
                    path.AddArc(photoRect.X, photoRect.Y + photoRect.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
                    path.CloseFigure();

                    // Llenar el área del path con color blanco para el fondo del rectángulo
                    using (var fillBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                    {
                        graphicsOriginal.FillPath(fillBrush, path);
                    }

                    using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.White, borderSize))
                    {
                        graphicsOriginal.DrawPath(borderPen, path);
                    }

                    // Ajustar el rectángulo para la foto, considerando el borde
                    System.Drawing.Rectangle photoRectWithBorder = System.Drawing.Rectangle.Inflate(photoRect, -borderSize, -borderSize);

                    graphicsOriginal.SetClip(path);
                    graphicsOriginal.DrawImage(foto, photoRectWithBorder);
                    graphicsOriginal.ResetClip();

                    // Define the text to be drawn on the image from the requestLicence object
                    string nombreCompleto = !string.IsNullOrEmpty(requestLicence.ShortName) ? requestLicence.ShortName : requestLicence.Name; // Reemplazar con la propiedad real de requestLicence
                    string numeroLicencia = requestLicence.LicenceNumber; // Reemplazar con la propiedad real de requestLicence
                                                                          // ... y así para el resto de los campos que necesitas mostrar

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
                        var imageWidth = fondoOriginal.Width;
                        var availableWidth = imageWidth - 30; // Espacio disponible considerando un margen de 30 píxeles a cada lado

                        // Divide el nombre en dos líneas si es necesario
                        var (firstLine, secondLine) = SplitName(nombreCompleto, availableWidth, graphicsOriginal, fontTitle);

                        var firstLineSize = graphicsOriginal.MeasureString(firstLine, fontTitle);
                        var nombreCompletoSize = graphicsOriginal.MeasureString(nombreCompleto, fontTitle);
                        var secondLineSize = graphicsOriginal.MeasureString(secondLine, fontTitle);
                        var licenceNumberSize = graphicsOriginal.MeasureString(numeroLicencia, fontBody);
                        var gradeSize = graphicsOriginal.MeasureString(requestLicence.Grade, fontBody);
                        var birthDateSize = graphicsOriginal.MeasureString(requestLicence.BirthDay.Value.ToString("d/M/yyyy"), fontBody);
                        var expeditionSize = graphicsOriginal.MeasureString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody);
                        var expirationSize = graphicsOriginal.MeasureString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody);
                        var fiaMSSize = graphicsOriginal.MeasureString(requestLicence.FiaMS.Value.ToString("d/M/yyyy"), fontBody);


                        // Dibujar los demás textos alineados a la derecha
                        var rightEdge = 900; // 30 píxeles de margen derecho

                        // Calcular las posiciones para centrar el texto
                        var centerX = imageWidth / 2;

                        // Dibujar el nombre completo centrado
                        //graphics.DrawString(firstLine, fontTitle, brush, new System.Drawing.PointF(centerX - firstLineSize.Width / 2, 980));
                        var nameSize = graphicsOriginal.MeasureString(nombreCompleto, fontName);
                        float nameX = (2084 - nameSize.Width) / 2;
                        graphicsOriginal.DrawString(nombreCompleto, fontName, brush, nameX, 1230);
                        //if (!string.IsNullOrEmpty(secondLine))
                        //{
                        //    graphicsOriginal.DrawString(secondLine, fontTitle, brush, new System.Drawing.PointF(centerX - secondLineSize.Width / 2, 355));
                        //}                                                
                        float currentY = 1503;
                        if (!string.IsNullOrEmpty(numeroLicencia))
                            graphicsOriginal.DrawString(numeroLicencia, fontBody, brush, rightEdge, currentY);

                        currentY += 142;


                        if (!string.IsNullOrEmpty(requestLicence.Grade))
                            graphicsOriginal.DrawString(requestLicence.Grade, fontBody, brush, rightEdge, currentY);

                        currentY += 144;

                        if (requestLicence.BirthDay != null)
                            graphicsOriginal.DrawString(requestLicence.BirthDay.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);

                        currentY += 142;


                        if (requestLicence.Expedition != null)
                            graphicsOriginal.DrawString(requestLicence.Expedition.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);

                        currentY += 142;


                        if (requestLicence.Expiration != null)
                            graphicsOriginal.DrawString(requestLicence.Expiration.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);

                        currentY += 690;


                        if (requestLicence.FiaMS != null)
                            graphicsOriginal.DrawString(requestLicence.FiaMS.Value.ToString("d/M/yyyy"), fontBody, brush, rightEdge, currentY);


                        int checkboxSize = 82;
                        System.Drawing.Point checkboxSiPosition = new System.Drawing.Point(910, 2330);
                        System.Drawing.Point checkboxNoPosition = new System.Drawing.Point(1095, 2330);

                        System.Drawing.Point checkboxSiPosition2 = new System.Drawing.Point(910, 2472);
                        System.Drawing.Point checkboxNoPosition2 = new System.Drawing.Point(1095, 2472);

                        System.Drawing.Point checkboxSiPosition3 = new System.Drawing.Point(910, 2609);
                        System.Drawing.Point checkboxNoPosition3 = new System.Drawing.Point(1095, 2609);


                        DrawCheckbox(graphicsOriginal, pen, brushBlack, fontEquis, checkboxSiPosition, checkboxSize, requestLicence.VistaCorregida);
                        DrawCheckbox(graphicsOriginal, pen, brushBlack, fontEquis, checkboxNoPosition, checkboxSize, !requestLicence.VistaCorregida);

                        DrawCheckbox(graphicsOriginal, pen, brushBlack, fontEquis, checkboxSiPosition2, checkboxSize, requestLicence.SupervisionMedica);
                        DrawCheckbox(graphicsOriginal, pen, brushBlack, fontEquis, checkboxNoPosition2, checkboxSize, !requestLicence.SupervisionMedica);

                        DrawCheckbox(graphicsOriginal, pen, brushBlack, fontEquis, checkboxSiPosition3, checkboxSize, requestLicence.ConsentimientoWADB);
                        DrawCheckbox(graphicsOriginal, pen, brushBlack, fontEquis, checkboxNoPosition3, checkboxSize, !requestLicence.ConsentimientoWADB);




                        // Genera el código QR con los datos deseados
                        //string datosCompletos =
                        //    $"Nombre Completo: {requestLicence.Name}\n" +
                        //    $"N. LICENCIA: {requestLicence.LicenceNumber}\n" +
                        //    $"GRADO: {requestLicence.Grade}\n" +
                        //    $"FECHA DE NACIMIENTO: {requestLicence.BirthDay.Value.ToString("d/M/yyyy")}\n" +
                        //    $"EXPEDICIÓN: {requestLicence.Expedition.Value.ToString("d/M/yyyy")}\n" +
                        //    $"VENCIMIENTO: {requestLicence.Expiration.Value.ToString("d/M/yyyy")}\n" +
                        //    $"VISTA CORREGIDA: {(requestLicence.VistaCorregida ? "SI" : "NO")}\n" +
                        //    $"SUPERVISIÓN MÉDICA: {(requestLicence.SupervisionMedica ? "SI" : "NO")}\n" +
                        //    $"CONSENTIMIENTO PARA EL PROCESAMIENTO DE DATOS PERSONALES EN LA WADB: {(requestLicence.ConsentimientoWADB ? "SI" : "NO")}\n" +
                        //    $"FIA M.S: {DateTime.Now.Date.ToString("d/M/yyyy")}";

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
                            int qrX = 1490; // Posición en el eje X
                            int qrY = 2305; // Posición en el eje Y
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
                    }

                    // Al final del método:
                    imageOriginalSize.Save(memoryStream, ImageFormat.Png);
                    return File(memoryStream.ToArray(), "image/png");
                }
            }

            return RedirectToAction(nameof(Index));

        }


        public async Task<IActionResult> CreateImageInternacionalReverso(int? id)
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

        //public async Task<IActionResult> UpdateFullApproved(int? id, int? filterTypeId, int? filterLicenceType)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var requestLicenceSportInternational = await _context.RequestLicenceSports.FindAsync(id);
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

            var requestLicenceSportInternational = await _context.RequestLicenceSports.FindAsync(id);
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
                    RequestLicenceSportId = requestLicenceSportInternational.RequestLicenceSportId                    
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



        public async Task<IActionResult> ChangeAssigned(int? id, int? filterTypeId, int? filterLicenceType)
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

            ViewData["UserId"] = new SelectList(
                  _context.Users.Where(u => !u.IsDelete)
                      .AsEnumerable()  // Cambia a operaciones de cliente en memoria                      
                      .ToList(),
                  "Id",
                  "Name"
              );


            var model = new RequestViewModelShort()
            {
                RequestId = requestLicenceSport.RequestLicenceSportId,                
                UserId = requestLicenceSport.UserId,
                FilterLicenceType=filterLicenceType,
                FilterTypeId= filterTypeId
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
                    var request = await _context.RequestLicenceSports.FindAsync(model.RequestId);

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


        public async Task<IActionResult> EditPartial(int? id, int? filterTypeId, int? filterLicenceType)
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
            var licenceTypes = Enum.GetValues(typeof(LicenceType))
                               .Cast<LicenceType>()
                               .Select(s => new SelectListItem
                               {
                                   Value = ((int)s).ToString(),
                                   Text = s.ToString()
                               })
                               .ToList();

            ViewBag.LicenceTypes = licenceTypes;


            var model = new RequestLicenceSportViewModelShort()
            {
                ConsentimientoWADB = requestLicenceSport.ConsentimientoWADB,
                Expedition = requestLicenceSport.Expedition,
                Expiration = requestLicenceSport.Expiration,
                FiaMS = requestLicenceSport.FiaMS,
                Grade = requestLicenceSport.Grade,
                LicenceType = requestLicenceSport.LicenceType,
                RequestLicenceSportId = requestLicenceSport.RequestLicenceSportId,
                ShortName = requestLicenceSport.ShortName,
                SupervisionMedica = requestLicenceSport.SupervisionMedica,
                VistaCorregida = requestLicenceSport.VistaCorregida,
                LicenceNumber= requestLicenceSport.LicenceNumber,
                FilterLicenceType = filterLicenceType,
                FilterTypeId = filterTypeId,
                Photo=requestLicenceSport.Photo,
                BirthDay=requestLicenceSport.BirthDay
            };

            if (model.ShortName == null)
                model.ShortName = requestLicenceSport.Name;

            return View("_EditPartial", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPartial(RequestLicenceSportViewModelShort model)
        {       
            if (ModelState.IsValid)
            {
                try
                {
                    var pic = model.Photo;
                    var guid = Guid.NewGuid();
                    var nombreArchivoPhoto = "PH_" + guid + ".jpg";

                    var requestLicenceSport = await _context.RequestLicenceSports.FindAsync(model.RequestLicenceSportId);

                    requestLicenceSport.ConsentimientoWADB = model.ConsentimientoWADB;
                    requestLicenceSport.Expedition = model.Expedition;
                    requestLicenceSport.Expiration = model.Expiration;
                    requestLicenceSport.FiaMS = model.FiaMS;
                    requestLicenceSport.Grade = model.Grade;
                    requestLicenceSport.LicenceType = model.LicenceType;
                    requestLicenceSport.RequestLicenceSportId = model.RequestLicenceSportId;
                    requestLicenceSport.ShortName = model.ShortName;
                    requestLicenceSport.SupervisionMedica = model.SupervisionMedica;
                    requestLicenceSport.VistaCorregida = model.VistaCorregida;
                    requestLicenceSport.LicenceNumber = model.LicenceNumber;
                    requestLicenceSport.BirthDay= model.BirthDay;

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
                    if (!RequestLicenceSportExists(model.RequestLicenceSportId))
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


        private async Task<IActionResult> GenerateLicenceImage(RequestLicenceSport requestLicence, string tipo)
        {
            if (requestLicence.LicenceType == LicenceType.Nacional)
            {
                if (tipo == "anverso")
                {
                    return await CreateImageAnverso(requestLicence.RequestLicenceSportId);
                }
            }
            else
            {
                if (tipo == "anverso")
                {
                    return await CreateImageInternacionalAnverso(requestLicence.RequestLicenceSportId);
                }
            }

            return null;
        }

        private async Task<IActionResult> GenerateLicenceImageEmail(RequestLicenceSport requestLicence, string tipo)
        {
            if (requestLicence.LicenceType == LicenceType.Nacional)
            {
                if (tipo == "anverso")
                {
                    return await CreateImageAnverso(requestLicence.RequestLicenceSportId);
                }

            }
            else
            {
                if (tipo == "anverso")
                {
                    return await CreateImageInternacionalAnverso(requestLicence.RequestLicenceSportId);
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
                graphics.DrawImage(fondo, 0, 0); // Dibuja el fondo primero
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

            var requestLicenceSportInternational = await _context.RequestLicenceSports.FindAsync(id);
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

                var anversoResult = await GenerateLicenceImage(requestLicenceSportInternational, "anverso");
                //var reversoResult = await GenerateLicenceImage(requestLicenceSportInternational, "reverso");

                string anversoPath = SaveImageFromResult(anversoResult, "anverso", requestLicenceSportInternational.RequestLicenceSportId);
                //string reversoPath = SaveImageFromResult(reversoResult, "reverso", requestLicenceSportInternational.RequestLicenceSportId);

                // Obtener el contenido de los archivos generados
                var anversoContent = System.IO.File.ReadAllBytes(anversoPath);
                //var reversoContent = System.IO.File.ReadAllBytes(reversoPath);

                // Crear una lista de adjuntos
                var attachments = new List<(string FileName, byte[] Content)>
                {
                     ($"Licencia Deportiva {(requestLicenceSportInternational.LicenceType == LicenceType.Nacional ? "Nacional" : "Internacional")}.png", anversoContent)
                    //("Reverso.png", reversoContent)
                };

                _mailHelper.SendMail(
                    requestLicenceSportInternational.Mail,
                    "Aprobación de Solicitud de licencia deportiva",
                    _configurationApp.MessageSolicitudApproved + GenerateHtmlTable(requestLicenceSportInternational, 1),
                    attachments // Adjuntos desde contenido
                );

                _mailHelper.SendMail(
                    _configuration["Mail:Admin"],
                    "Aprobación de Solicitud de licencia deportiva",
                    _configurationApp.MessageSolicitudAdminAprobado + GenerateHtmlTable(requestLicenceSportInternational, 1)
                );
            }
            else
            {
                requestLicenceSportInternational.Approved = null;
            }

            requestLicenceSportInternational.Modify = DateTime.Now;

            _context.Update(requestLicenceSportInternational);
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

            string directoryPath = Path.Combine(_webHostEnvironment.WebRootPath, "files/licences");
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
