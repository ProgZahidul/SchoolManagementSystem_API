using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SchoolApp.Models.DataModels
{

    public class AcademicMonth
    {
        [Key]
        public int MonthId { get; set; }
        public String? MonthName { get; set; }

    }

}
