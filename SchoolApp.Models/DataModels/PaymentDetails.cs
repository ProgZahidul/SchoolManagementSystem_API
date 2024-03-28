using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolApp.Models.DataModels
{
    public class PaymentDetails
    {
        [Key]
        public int PaymentDetailsId { get; set; }
        public int PaymentId { get; set; }
        public string MonthName { get; set; }
    }
}
