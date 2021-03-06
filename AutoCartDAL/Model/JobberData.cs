﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCarOperations.Model
{
    [Table("ck_downloaded_variant")]
    public class DownloadVariant
    {
        public string VariantID { get; set; }
        public string Make_Descr { get; set; }
        public string Model_Descr { get; set; }
        public int From_Year { get; set; }
        public int To_Year { get; set; }
        public string Option { get; set; }
        public string Position { get; set; }
        public string Product_Descr { get; set; }
        public string ProductID { get; set; }
        public string Bodytype { get; set; }
        public string SubModel { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AppDataId { get; set; }
        public string BaseVehicleID { get; set; }
        public string RecID { get; set; }
    }
    [Table("ck_downloaded_item")]
    public class DownloadedItem
    {
        public string ItemID { get; set; }
        public string Description { get; set; }
        public decimal? Jobber { get; set; }
        public string UPC_Code { get; set; }
        public double? Gross_Weight { get; set; }
        public int Length { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Product_Family_ID { get; set; }
        public string Product_Family_Description { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JobberId { get; set; }

        public string ProductID { get; set; }
        public string MaterialID { get; set; }
    }
    [Table("ck_amazon_variant")]
    public class AmazonVariant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AmazonVariantId { get; set; }
        public string ASIN { get; set; }
        public string SKUOrUPC { get; set; }
        public string NavItem { get; set; }
        public string NavVariant { get; set; }
        public string ForceBlock { get; set; }
        public string ResourceCode { get; set; }
    }
    [Table("ck_product_master")]
    public class ProductMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string product_id { get; set; }
        public int product_family_id { get; set; }
    }
}
