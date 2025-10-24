using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopThoiTrangNam.Models;

namespace ShopThoiTrangNam.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var categories = _context.Categories.Include(c => c.Parent);
            return View(await categories.ToListAsync());
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            ViewData["ParentId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            ViewBag.NoParentOption = new SelectListItem()
            {
                Value = "",
                Text = "Không có danh mục cha"
            };
            return View(new Category()); 
        }


        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,CategoryName,ParentId,Description")] Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateParentCategoriesDropDown(category.ParentId);
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            PopulateParentCategoriesDropDown(category.ParentId, category.CategoryId); // loại bỏ chính nó để tránh vòng lặp
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,CategoryName,ParentId,Description")] Category category)
        {
            if (id != category.CategoryId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            PopulateParentCategoriesDropDown(category.ParentId, category.CategoryId);
            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound();

            var hasChildren = _context.Categories.Any(c => c.ParentId == id);
            if (hasChildren)
            {
                ModelState.AddModelError("", "Không thể xóa vì danh mục này có danh mục con.");
                return View(category); // lúc này category đã có dữ liệu
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra tồn tại
        private bool CategoryExists(int id) => _context.Categories.Any(e => e.CategoryId == id);

        // Hàm tiện ích để populate dropdown danh mục cha
        private void PopulateParentCategoriesDropDown(int? selectedParentId = null, int? excludeId = null)
        {
            var categoriesQuery = _context.Categories.AsQueryable();

            if (excludeId.HasValue)
            {
                categoriesQuery = categoriesQuery.Where(c => c.CategoryId != excludeId.Value);
            }

            var categoriesList = categoriesQuery
                .OrderBy(c => c.CategoryName)
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                })
                .ToList();

            // Thêm option mặc định "Không có danh mục cha"
            categoriesList.Insert(0, new SelectListItem { Value = "", Text = "-- Không có danh mục cha --" });

            ViewBag.ParentId = new SelectList(categoriesList, "Value", "Text", selectedParentId);
        }
    }
}
