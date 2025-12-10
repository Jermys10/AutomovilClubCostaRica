using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AutomovilClub.Backend.Data;
using AutomovilClub.Backend.Data.Entities;

namespace AutomovilClub.Backend.Controllers
{
    public class ConfigurationsController : Controller
    {
        private readonly DataContext _context;
      

        public ConfigurationsController(DataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Configurations.ToListAsync());
        }

       

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var configuration = await _context.Configurations
                .FirstOrDefaultAsync(m => m.ConfigurationId == id);
            if (configuration == null)
            {
                return NotFound();
            }

            return View(configuration);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Data.Entities.Configuration configuration)
        {
            if (ModelState.IsValid)
            {
                _context.Add(configuration);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(configuration);
        }

        public async Task<IActionResult> Edit()
        {
            var configuration = await _context.Configurations.ToListAsync();
            if (configuration == null)
            {
                return NotFound();
            }

            string filePath = "wwwroot/padron/PADRON_COMPLETO.txt"; // Reemplaza con la ruta correcta de tu archivo
            //await ReadFromFile(filePath);
           
           

            return View(configuration[0]);
        }

        public async Task ReadFromFile(string filePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        try 
	                    {	        
		                    string[] parts = line.Split(',');
                            if (parts.Length == 8)
                            {
                                if (!ExistPerson(parts[0])) 
                                {
                                    Person person = new Person
                                    {
                                        Identification = parts[0],
                                        District = parts[1],
                                        Expirate = parts[3],
                                        Name = parts[5].Trim(),
                                        LastName1 = parts[6].Trim(),
                                        LastName2 = parts[7].Trim()
                                    };

                                    _context.People.Add(person);

                                    await _context.SaveChangesAsync();
                                }
                            
                            }
                            else
                            {
                                Console.WriteLine($"Error: La línea '{line}' no tiene el formato esperado.");
                            }
	                    }
	                    catch (Exception ex)
                        {
                            Console.WriteLine($"Error al leer el archivo Error: La línea '{line}' : {ex.Message}");
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer el archivo: {ex.Message}");
            }
        }

        public bool ExistPerson(string identification) 
        {
            try 
	        {
                var person = _context.People.Any(p => p.Identification == identification);

                return person;
            }
	        catch (Exception ex)
	        {
		
		        Console.WriteLine($"Error al leer el archivo: {ex.Message}");
              
            }
            return false;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Data.Entities.Configuration configuration)
        {          
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(configuration);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConfigurationExists(configuration.ConfigurationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Edit));
            }
            return View(configuration);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

         
            var configuration = await _context.Configurations.FindAsync(id);
            if (configuration != null)
            {
                _context.Configurations.Remove(configuration);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ConfigurationExists(int id)
        {
            return _context.Configurations.Any(e => e.ConfigurationId == id);
        }
    }
}
