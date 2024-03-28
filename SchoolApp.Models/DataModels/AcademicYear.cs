using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SchoolApp.Models.DataModels
{


    public class AcademicYear
    {
        public int AcademicYearId { get; set; }
        public required string Name { get; set; }


    }
}
