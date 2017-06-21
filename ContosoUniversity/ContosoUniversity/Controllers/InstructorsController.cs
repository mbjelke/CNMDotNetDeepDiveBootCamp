using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Models.ViewModels;

namespace ContosoUniversity.Controllers
{
	public class InstructorsController : Controller
	{
		private readonly ApplicationDbContext _context;

		public InstructorsController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: Instructors
		public async Task<IActionResult> Index(int? id, int? courseId)
		{
			var viewModel = new InstructorIndex();

			// showing ALL THE THINGS re: Instructors
			// AsNoTracking applies to the CONTEXT (here) but not the viewModel (below)
			viewModel.Instructors = await _context.Instructors
											.Include(i => i.OfficeAssignment)
											.Include(i => i.CourseAssignments)
											// .ThenInclude(i => i.Course)
											// 	.ThenInclude(i => i.Enrollments)
											//		.ThenInclude(i => i.Student)
											.Include(i => i.CourseAssignments)
												.ThenInclude(i => i.Course)
													.ThenInclude(i => i.Department)
											//.AsNoTracking()
											.OrderBy(i => i.LastName)

											.ToListAsync();
			if (id != null)
			{
				ViewData["InstructorId"] = id.Value;
				Instructor instructor = viewModel.Instructors
												.Where(i => i.ID == id.Value)
												.Single();  // converting the IEnumerable of Where to a single Instructor

				viewModel.Courses = instructor.CourseAssignments
											.Select(s => s.Course);
			}

			if (courseId != null)
			{
				ViewData["CourseId"] = courseId.Value;
				// Here we are looking for a specific course ID and listing the enrollments for that one course

				// for loading data eagerly
				//viewModel.Enrollments = viewModel.Courses
				//							.Where(c => c.CourseID == courseId.Value)
				//							.Single()
				//							.Enrollments;


				// for loading data explicitly
				var selectedCourse = viewModel.Courses
												.Where(x => x.CourseID == courseId)
												.Single();
				// Entry may not work if .AsNoTracking() is specified on data context
				await _context.Entry(selectedCourse)
								.Collection(x => x.Enrollments).LoadAsync();

				foreach (Enrollment enrollment in selectedCourse.Enrollments)
				{
					await _context.Entry(enrollment)
									.Reference(x => x.Student)
									.LoadAsync();
				}
				viewModel.Enrollments = selectedCourse.Enrollments;
			}



			return View(viewModel);
		}

		// GET: Instructors/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var instructor = await _context.Instructors
				.SingleOrDefaultAsync(m => m.ID == id);
			if (instructor == null)
			{
				return NotFound();
			}

			return View(instructor);
		}

		// GET: Instructors/Create
		public IActionResult Create()
		{
			return View();
		}

		// POST: Instructors/Create
		// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("ID,LastName,FirstMidName,HireDate")] Instructor instructor)
		{
			if (ModelState.IsValid)
			{
				_context.Add(instructor);
				await _context.SaveChangesAsync();
				return RedirectToAction("Index");
			}
			return View(instructor);
		}

		// GET: Instructors/Edit/5
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var instructor = await _context.Instructors
										.Include(i => i.OfficeAssignment)
										.Include(i => i.CourseAssignments)
											.ThenInclude(i => i.Course)
										.AsNoTracking()
										.SingleOrDefaultAsync(m => m.ID == id);
			if (instructor == null)
			{
				return NotFound();
			}

			PopulateAssignedCourseData(instructor);

			return View(instructor);
		}



		// POST: Instructors/Edit/5
		// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, string[] selectedCourses,
			[Bind("ID,LastName,FirstMidName,HireDate")] Instructor instructor)
		{
			if (id != instructor.ID)
			{
				return NotFound();
			}

			var instructorToUpdate = _context.Instructors
											.Where(i=>i.ID== instructor.ID)
											.Include(i => i.CourseAssignments)
											.Single();
			
			if (ModelState.IsValid)
			{
				UpdateInstructorCourses(selectedCourses, instructorToUpdate);
				try
				{
					_context.Update(instructor);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!InstructorExists(instructor.ID))
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

			UpdateInstructorCourses(selectedCourses, instructorToUpdate);
			PopulateAssignedCourseData(instructor);
			return View(instructor);
		}

		private void UpdateInstructorCourses(string[] selectedCourses, Instructor instructor)
		{
			if (selectedCourses == null)
			{
				instructor.CourseAssignments = new List<CourseAssignment>();
				return;
			}

			var selectedCoursesHS = new HashSet<string>(selectedCourses);
			var instructorCourses = new HashSet<int>
				(instructor.CourseAssignments.Select(c => c.Course.CourseID));
			foreach (var course in _context.Courses)
			{
				if (selectedCoursesHS.Contains(course.CourseID.ToString()))
				{
					if (!instructorCourses.Contains(course.CourseID))
					{
						instructor.CourseAssignments.Add(
							new CourseAssignment
							{
								InstructorID = instructor.ID,
								CourseID = course.CourseID}
							);
					}
				}
				else
				{

					if (instructorCourses.Contains(course.CourseID))
					{
						CourseAssignment courseToRemove = instructor.CourseAssignments.SingleOrDefault(i => i.CourseID == course.CourseID);
						_context.Remove(courseToRemove);
					}
				}
			}

		}

		// GET: Instructors/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var instructor = await _context.Instructors
				.SingleOrDefaultAsync(m => m.ID == id);
			if (instructor == null)
			{
				return NotFound();
			}

			return View(instructor);
		}

		// POST: Instructors/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var instructor = await _context.Instructors.SingleOrDefaultAsync(m => m.ID == id);
			_context.Instructors.Remove(instructor);
			await _context.SaveChangesAsync();
			return RedirectToAction("Index");
		}

		private bool InstructorExists(int id)
		{
			return _context.Instructors.Any(e => e.ID == id);
		}

		private void PopulateAssignedCourseData(Instructor instructor)
		{
			var allCourses = _context.Courses;
			var instructorCourses = new HashSet<int>(instructor.CourseAssignments.Select(c => c.CourseID));
			var viewModel = new List<InstructorAssignedCourse>();
			foreach (var course in allCourses)
			{
				viewModel.Add(new InstructorAssignedCourse
				{
					CourseID = course.CourseID,
					Title = course.Title,
					Assigned = instructorCourses.Contains(course.CourseID)
				});
			}
			ViewData["Courses"] = viewModel;
		}
	}
}
