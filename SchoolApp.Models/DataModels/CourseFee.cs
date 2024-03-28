namespace SchoolApp.Models.DataModels
{
    public class CourseFee
    {
        public int CourseFeeId { get; set; }

        public required string CourseName { get; set; }

        public decimal TotalCourseFeeAmount { get; set; }
        public IList<FeeStructure>? feeStructures { get; set; }
        public IList<CourseFeeDetails>? courseFeeDetails { get; set; }
        
    }
}
