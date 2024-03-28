using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolApp.DAL.SchoolContext;
using SchoolApp.Models.DataModels;

namespace SchoolApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourseFeesController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public CourseFeesController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: api/CourseFee
        [HttpGet]
        public async Task<IActionResult> GetAllCourseFees()
        {
            try
            {
                var courseFees = await _context.dbsCourseFees
                    .Include(cf => cf.courseFeeDetails) // Include related CourseFeeDetails if needed
                    .ToListAsync();

                return Ok(courseFees);
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"Exception: {ex}");

                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }



        [HttpPost]
        public async Task<IActionResult> CreateCourseFee([FromBody] CourseFee courseFee)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    await AttachFeeStructuresAsync(courseFee);
                    //CalculateCourseFeeFields(courseFee);

                    _context.dbsCourseFees.Add(courseFee);
                    await _context.SaveChangesAsync();

                    SaveCourseFeeDetails(courseFee); // Call SaveCourseFeeDetails method

                    transaction.Commit();

                    return Ok(courseFee);
                }
                catch (Exception ex)
                {
                    // Log the exception for debugging purposes
                    Console.WriteLine($"Exception: {ex}");

                    transaction.Rollback();
                    return StatusCode(500, $"Internal Server Error: {ex.Message}");
                }
            }
        }

        private async Task AttachFeeStructuresAsync(CourseFee courseFee)
        {
            if (courseFee.feeStructures != null && courseFee.feeStructures.Any())
            {
                courseFee.feeStructures = await _context.dbsFeeStructure
                    .Where(fs => courseFee.feeStructures.Select(f => f.FeeStructureId).Contains(fs.FeeStructureId))
                    .ToListAsync();
            }
        }


        private void CalculateCourseFeeFields(CourseFee courseFee)
        {
            courseFee.TotalCourseFeeAmount = courseFee.feeStructures?.Sum(fs => fs.FeeAmount) ?? 0;
        }
        private void SaveCourseFeeDetails(CourseFee courseFee)
        {
            if (courseFee.feeStructures != null && courseFee.feeStructures.Any())
            {
                foreach (var feeStructure in courseFee.feeStructures)
                {
                    var courseFeeDetail = new CourseFeeDetails
                    {
                        CourseFeeId = courseFee.CourseFeeId,
                        FeeAmount = feeStructure.FeeAmount,
                        FeeTypeName = feeStructure.TypeName // Set FeeTypeName based on FeeStructure
                    };

                    _context.dbsCourseFeeDetails.Add(courseFeeDetail);
                }

                _context.SaveChanges();
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourseFee(int id, [FromBody] CourseFee updatedCourseFee)
        {
            if (id != updatedCourseFee.CourseFeeId)
            {
                return BadRequest("ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var existingCourseFee = await _context.dbsCourseFees
                        .Include(cf => cf.courseFeeDetails)
                        .FirstOrDefaultAsync(cf => cf.CourseFeeId == id);

                    if (existingCourseFee == null)
                    {
                        return NotFound($"CourseFee with ID {id} not found.");
                    }

                    // Update properties of the existing course fee
                    existingCourseFee.CourseName = updatedCourseFee.CourseName;
                    existingCourseFee.TotalCourseFeeAmount = updatedCourseFee.TotalCourseFeeAmount;
                    // Update other properties as needed

                    // Clear existing fee structures
                    existingCourseFee.courseFeeDetails.Clear();

                    // Attach fee structures from updated course fee
                    await AttachFeeStructuresAsync(existingCourseFee, updatedCourseFee);

                    // Recalculate course fee fields
                    CalculateCourseFeeFields(existingCourseFee);

                    // Save changes to the database
                    await _context.SaveChangesAsync();
                    SaveCourseFeeDetails(existingCourseFee);

                    transaction.Commit();

                    return Ok(existingCourseFee);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex}");
                    transaction.Rollback();
                    return StatusCode(500, $"Internal Server Error: {ex.Message}");
                }
            }
        }

        private async Task AttachFeeStructuresAsync(CourseFee existingCourseFee, CourseFee updatedCourseFee)
        {
            if (updatedCourseFee.feeStructures != null && updatedCourseFee.feeStructures.Any())
            {
                existingCourseFee.feeStructures = await _context.dbsFeeStructure
                    .Where(fs => updatedCourseFee.feeStructures.Select(f => f.FeeStructureId).Contains(fs.FeeStructureId))
                    .ToListAsync();
            }
        }

       

        // GET: api/CourseFee/{id}
        // GET: api/CourseFee/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourseFeeById(int id)
        {
            try
            {
                var courseFee = await _context.dbsCourseFees
                    .Include(cf => cf.courseFeeDetails) // Include related CourseFeeDetails
                    .FirstOrDefaultAsync(cf => cf.CourseFeeId == id);

                if (courseFee == null)
                {
                    return NotFound($"CourseFee with ID {id} not found");
                }

                return Ok(courseFee);
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"Exception: {ex}");

                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourseFee(int id)
        {
            var courseFee = await _context.dbsCourseFees
                .Include(cf => cf.feeStructures)
                .FirstOrDefaultAsync(cf => cf.CourseFeeId == id);

            if (courseFee == null)
            {
                return NotFound($"CourseFee with ID {id} not found");
            }

            // Remove the reference to CourseFee in feeStructures
            foreach (var feeStructure in courseFee.feeStructures)
            {
                feeStructure.CourseFee = null;
            }

            _context.dbsCourseFees.Remove(courseFee);
            await _context.SaveChangesAsync();

            return NoContent();
        }



        // Other actions...

        private bool CourseFeeExists(int id)
        {
            return _context.dbsCourseFees.Any(e => e.CourseFeeId == id);
        }
    }
}

