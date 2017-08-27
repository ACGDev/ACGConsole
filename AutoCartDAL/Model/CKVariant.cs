using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCarOperations.Model
{
    public class CKVariant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        public string VariantID  {get;set;}
        public string Make_Descr { get;set;}
        public string Model_Descr { get;set;}
        public int? From_Year { get;set;}
        public int? To_Year { get;set;}
        public string Option { get;set;}
        public string ItemID { get;set;}
        public double? Jobber_Price { get;set;}
        public string Position { get;set;}
        public string ProductID { get;set;}
        public string BodyType { get;set;}
        public string SubModel { get;set;}
    }
}
