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
        public string SKU { get; set; }
        public string ItemID { get; set; }
        public string VariantId { get; set; }
        public int? InventoryQty { get;set;}
        public string OEM { get;set;}
        public string Blocked { get;set;}
        public string ProductCode { get;set;}
        public string ProductFamily { get;set;}
    }
}
