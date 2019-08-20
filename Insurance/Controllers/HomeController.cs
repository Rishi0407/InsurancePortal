using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Insurance.Models;
using Insurance.Models.ViewModel;
using System.IO;
using Insurance.Helper;
using Microsoft.Extensions.Configuration;

namespace Insurance.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration iconfiguration;

        public HomeController(IConfiguration configuration)
        {
            this.iconfiguration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
           if(ModelState.IsValid)
            {
                var customerId = Guid.NewGuid();
                StorageHelper storageHelper = new StorageHelper();
                storageHelper.ConnectionString = iconfiguration.GetConnectionString("StorageConnection");
                //Save Customer image to Azure Blob
                var tempFile = Path.GetTempFileName();
                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    await model.Image.CopyToAsync(fs);
                }
                var fileName = Path.GetFileName(model.Image.FileName);
                var tempPath = Path.GetDirectoryName(tempFile);
                var imagPath = Path.Combine(tempPath, string.Concat(customerId+ "_"+ fileName));
                System.IO.File.Move(tempFile, imagPath); //rename temp file
              var imageUrl =  await storageHelper.UploadCustomerImageAsync("images", imagPath);
                //save customer data to Azure Table
                Customer customer = new Customer(customerId.ToString(), model.InsuranceType);
                customer.FullName = model.FullName;
                customer.Email = model.Email;
                customer.Amount = model.Amount;
                customer.AppDate = model.AppDate;
                customer.EndDate = model.EndDate;
               var Customer = storageHelper.InsertCustomerAsync("customer", customer);
                //Add a configuration message to Azure Queue

                storageHelper.AddMessageAsync("insurance-request", customer);
                return RedirectToAction("Index");
            }
            else
            {
                return View();
            }
           
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
