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
    public class PaymentsController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public PaymentsController(SchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetAllPayments()
        {
            return await _context.dbsPayments
                .Include(p => p.PaymentDetails) // Include related PaymentDetails
                .ToListAsync();
        }

        // GET: api/Payments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPaymentById(int id)
        {
            var payment = await _context.dbsPayments
                .Include(p => p.PaymentDetails) // Include related PaymentDetails
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            return payment;
        }
    

    [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] Payment payment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    await AttachAcademicMonthAsync(payment);

                    await CalculatePaymentFieldsAsync(payment);
                    UpdateDueBalance(payment);

                    _context.dbsPayments.Add(payment);
                    await _context.SaveChangesAsync();
                    SavePaymentDetails(payment);
                    transaction.Commit();

                    return Ok(payment);
                }
                catch (Exception ex)
                {
                    //Log the exception for debugging purposes

                    Console.WriteLine($"Exception: {ex}");

                    transaction.Rollback();
                    return StatusCode(500, $"Internal Server Error: {ex.Message}");
                }
            }
        }

        private async Task CalculatePaymentFieldsAsync(Payment payment)
        {
            var student = await _context.dbsStudent
                .Where(s => s.StudentId == payment.StudentId)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                throw new Exception("Invalid Student Id: " + payment.StudentId);
            }

            payment.StudentName = student.StudentName;

            var courseFee = await _context.dbsCourseFees
                .Where(cf => cf.CourseFeeId == payment.CourseFeeId)
                .FirstOrDefaultAsync();

            if (courseFee == null)
            {
                throw new Exception("Invalid Course Fee Id: " + payment.CourseFeeId);
            }

            // Calculate TotalFeeAmount based on the count of academic months
            var academicMonthsCount = payment.academicMonths?.Count ?? 0;
            payment.TotalFeeAmount = courseFee.TotalCourseFeeAmount * academicMonthsCount;

            // Calculate total amount with applied discount
            payment.TotalAmount = payment.TotalFeeAmount - (payment.TotalFeeAmount * (payment.Waver / 100)) + payment.PreviousDue;

            var dueBalance = await _context.dbsDueBalance
                .Where(db => db.StudentId == payment.StudentId)
                .Select(db => db.DueBalanceAmount)
                .FirstOrDefaultAsync();

            payment.PreviousDue = dueBalance ?? 0;
            payment.AmountRemaining = payment.TotalAmount - payment.AmountPaid;
        }


        private void UpdateDueBalance(Payment payment)
        {
            var dueBalance = _context.dbsDueBalance
                .Where(db => db.StudentId == payment.StudentId)
                .FirstOrDefault();

            if (dueBalance != null)
            {
                dueBalance.DueBalanceAmount = payment.AmountRemaining;
                dueBalance.LastUpdate = DateTime.Now; // Update LastUpdate timestamp
            }
            else
            {
                _context.dbsDueBalance.Add(new DueBalance
                {
                    StudentId = payment.StudentId,
                    DueBalanceAmount = payment.AmountRemaining,
                    LastUpdate = DateTime.Now // Set LastUpdate timestamp for a new record
                });
            }

            _context.SaveChanges(); // Save changes to the database
        }

       

        private async Task AttachAcademicMonthAsync(Payment payment)
        {
            if (payment.academicMonths != null && payment.academicMonths.Any())
            {
                payment.academicMonths = await _context.dbsAcademicMonths
                    .Where(am => payment.academicMonths.Select(m => m.MonthId).Contains(am.MonthId))
                    .ToListAsync();
            }
        }

        private void SavePaymentDetails(Payment payment)
        {
            if (payment.academicMonths != null && payment.academicMonths.Any())
            {
                foreach (var academicMonth in payment.academicMonths)
                {
                    var paymentDetail = new PaymentDetails
                    {
                        PaymentId = payment.PaymentId,
                        MonthName = academicMonth.MonthName
                    };

                    _context.dbsPaymentDetails.Add(paymentDetail);
                }

                _context.SaveChanges();
            }
        }




        // PUT: api/Payments/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePayment(int id, [FromBody] Payment updatedPayment)
        {
            if (id != updatedPayment.PaymentId)
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
                    var existingPayment = await _context.dbsPayments
                        .Include(p=> p.CourseFees)
                        .Include(p => p.PaymentDetails)
                        //.Include(p => p.DueBanlace)
                        .FirstOrDefaultAsync(p => p.PaymentId == id);

                    if (existingPayment == null)
                    {
                        return NotFound($"Payment with ID {id} not found.");
                    }

                    // Update properties of the existing payment
                    existingPayment.StudentId = updatedPayment.StudentId;
                   
                    existingPayment.StudentName = updatedPayment.StudentName;
                    existingPayment.CourseFeeId = updatedPayment.CourseFeeId;
                    existingPayment.TotalFeeAmount = updatedPayment.TotalFeeAmount;
                    existingPayment.Waver = updatedPayment.Waver;
                    existingPayment.PreviousDue = updatedPayment.PreviousDue;
                    existingPayment.AmountPaid = updatedPayment.AmountPaid;

                   
                    existingPayment.PaymentDetails.Clear();
                    //existingPayment.DueBalance.Clear();

                    // Attach payment details from updated payment
                    AttachPaymentDetails(existingPayment, updatedPayment);
                    await CalculatePaymentFieldsAsync(existingPayment);
                    UpdateDueBalance(existingPayment);

                    // Recalculate payment fields if needed

                    // Save changes to the database
                    await _context.SaveChangesAsync();

                    SavePaymentDetails(existingPayment);

                    transaction.Commit();

                    return Ok(existingPayment);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex}");
                    transaction.Rollback();
                    return StatusCode(500, $"Internal Server Error: {ex.Message}");
                }
            }
        }

        private void AttachPaymentDetails(Payment existingPayment, Payment updatedPayment)
        {
            if (updatedPayment.PaymentDetails != null && updatedPayment.PaymentDetails.Any())
            {
                existingPayment.PaymentDetails = updatedPayment.PaymentDetails.ToList();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(int id)
        {
            var payment = await _context.dbsPayments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            _context.dbsPayments.Remove(payment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PaymentExists(int id)
        {
            return _context.dbsPayments.Any(e => e.PaymentId == id);
        }
    }
}
