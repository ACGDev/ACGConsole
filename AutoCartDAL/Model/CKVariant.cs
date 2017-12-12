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

    public class TempCKVariant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string SKU { get; set; }
        public string ItemID { get; set; }
        public string VariantId { get; set; }
        public int? InventoryQty { get; set; }
        public string OEM { get; set; }
        public string Blocked { get; set; }
        public string ProductCode { get; set; }
        public string ProductFamily { get; set; }
    }
    [Table("jfw_dealeritemprice")]
    public class DealerItemPrice
    {
        [Key]
        [Column(Order =1)]
        public string DealerCode { get; set; }
        public string DealerEmail { get; set; }
        public string Brand { get; set; }
        [Key]
        [Column(Order = 2)]
        public string ItemID { get; set; }
        public double MSRP { get; set; }
        public int DiscPct { get; set; }
        public int ShipCost { get; set; }
        public double CostToDealer { get; set; }
    }
}
