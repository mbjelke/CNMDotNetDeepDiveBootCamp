using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Phones.Models;

namespace Phones.Controllers
{
    public class PhonesController : Controller
    {
        private readonly PhonesContext _context;

        public PhonesController(PhonesContext context)
        {
            _context = context;    
        }

        // GET: Phones  //The view is passing the phoneOSSearch and phoneMfg to the controller here
        public async Task<IActionResult> Index(string phoneOSSearch, string phoneMfg)
        {
            var mfgs = from m in _context.Phone
                       orderby m.Mfg
                       select m.Mfg;

            var phones = from p in _context.Phone select p;

            if (!String.IsNullOrEmpty(phoneOSSearch))
            {
                phones = phones.Where(p => p.OS.Contains(phoneOSSearch));
            }
            if (!String.IsNullOrEmpty(phoneMfg))
            {
                phones = phones.Where(p => p.Mfg == phoneMfg);
            }

            var phoneViewModel = new PhoneMfgViewModel();
            phoneViewModel.phones = await phones.ToListAsync();
            phoneViewModel.mfgs = new SelectList(await mfgs.Distinct().ToListAsync());
            
            return View(phoneViewModel);
        }

        // GET: Phones/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phone = await _context.Phone
                .SingleOrDefaultAsync(m => m.ID == id);
            if (phone == null)
            {
                return NotFound();
            }

            return View(phone);
        }

        // GET: Phones/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Phones/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Color,Model,Size,CameraPix,Mfg,Price,OS,Condition,Release")] Phone phone)
        {
            if (ModelState.IsValid)
            {
                _context.Add(phone);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(phone);
        }

        // GET: Phones/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phone = await _context.Phone.SingleOrDefaultAsync(m => m.ID == id);
            if (phone == null)
            {
                return NotFound();
            }
            return View(phone);
        }

        // POST: Phones/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Color,Model,Size,CameraPix,Mfg,Price,OS,Condition,Release")] Phone phone)
        {
            if (id != phone.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phone);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhoneExists(phone.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
            return View(phone);
        }

        // GET: Phones/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phone = await _context.Phone
                .SingleOrDefaultAsync(m => m.ID == id);
            if (phone == null)
            {
                return NotFound();
            }

            return View(phone);
        }

        // POST: Phones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var phone = await _context.Phone.SingleOrDefaultAsync(m => m.ID == id);
            _context.Phone.Remove(phone);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        private bool PhoneExists(int id)
        {
            return _context.Phone.Any(e => e.ID == id);
        }
    }
}
