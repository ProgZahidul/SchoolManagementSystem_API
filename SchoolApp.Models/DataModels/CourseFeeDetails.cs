using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolApp.Models.DataModels
{
    public class CourseFeeDetails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CourseFeeDetailsId { get; set; }
        public int CourseFeeId { get; set; }
        public string FeeTypeName { get; set; }
        public decimal FeeAmount { get; set; }
    }
}
