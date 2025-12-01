using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StationaryManagement.Data;
using StationaryManagement.Models;

namespace StationaryManagement.Controllers
{
    public class StationeryRequestsController : Controller
    {
        private readonly AppDBContext _context;

        public StationeryRequestsController(AppDBContext context)
        {
            _context = context;
        }

        // GET: StationeryRequests
        public async Task<IActionResult> Index()
        {
            var appDBContext = _context.StationeryRequests.Include(s => s.Employee).Include(s => s.Superior);
            return View(await appDBContext.ToListAsync());
        }

        // GET: StationeryRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stationeryRequest = await _context.StationeryRequests
                .Include(s => s.Employee)
                .Include(s => s.Superior)
                .FirstOrDefaultAsync(m => m.RequestId == id);
            if (stationeryRequest == null)
            {
                return NotFound();
            }

            return View(stationeryRequest);
        }

        // GET: StationeryRequests/Create
        public IActionResult Create()
        {
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeId");
            ViewData["SuperiorId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeId");
            return View();
        }

        // POST: StationeryRequests/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RequestId,EmployeeId,SuperiorId,RequestDate,FromDate,ToDate,Status,TotalCost,Reason,LastStatusChangedAt")] StationeryRequest stationeryRequest)
        {
            if (ModelState.IsValid)
            {
                _context.Add(stationeryRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeId", stationeryRequest.EmployeeId);
            ViewData["SuperiorId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeId", stationeryRequest.SuperiorId);
            return View(stationeryRequest);
        }

        // GET: StationeryRequests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stationeryRequest = await _context.StationeryRequests.FindAsync(id);
            if (stationeryRequest == null)
            {
                return NotFound();
            }
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeId", stationeryRequest.EmployeeId);
            ViewData["SuperiorId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeId", stationeryRequest.SuperiorId);
            return View(stationeryRequest);
        }

        // POST: StationeryRequests/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RequestId,EmployeeId,SuperiorId,RequestDate,FromDate,ToDate,Status,TotalCost,Reason,LastStatusChangedAt")] StationeryRequest stationeryRequest)
        {
            if (id != stationeryRequest.RequestId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(stationeryRequest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StationeryRequestExists(stationeryRequest.RequestId))
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
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeId", stationeryRequest.EmployeeId);
            ViewData["SuperiorId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeId", stationeryRequest.SuperiorId);
            return View(stationeryRequest);
        }

        // GET: StationeryRequests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stationeryRequest = await _context.StationeryRequests
                .Include(s => s.Employee)
                .Include(s => s.Superior)
                .FirstOrDefaultAsync(m => m.RequestId == id);
            if (stationeryRequest == null)
            {
                return NotFound();
            }

            return View(stationeryRequest);
        }

        // POST: StationeryRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var stationeryRequest = await _context.StationeryRequests.FindAsync(id);
            if (stationeryRequest != null)
            {
                _context.StationeryRequests.Remove(stationeryRequest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StationeryRequestExists(int id)
        {
            return _context.StationeryRequests.Any(e => e.RequestId == id);
        }
    }
}
